using System.Collections.Immutable;
using System.Numerics;
using Newtonsoft.Json;
using Timepiece.Angler.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
///   Representation of the routing policies of all nodes in an Angler-produced network.
/// </summary>
public class AnglerNetwork
{
  [JsonConstructor]
  public AnglerNetwork(Dictionary<string, NodeProperties> nodes,
    List<ExternalPeer> externals)
  {
    Nodes = nodes;
    Externals = externals;
    Validate();
  }

  /// <summary>
  ///   The nodes of the network with their associated policies.
  /// </summary>
  [JsonRequired]
  public Dictionary<string, NodeProperties> Nodes { get; }

  /// <summary>
  ///   Symbolic expressions.
  /// </summary>
  public List<ExternalPeer> Externals { get; }

  /// <summary>
  ///   Verify the network for possible topology errors.
  ///   The network must have an edge to and from every node.
  /// </summary>
  /// <exception cref="Exception"></exception>
  public void Validate()
  {
    var errors = false;
    foreach (var peer in Externals)
    {
      var name = peer.Name;
      foreach (var node in peer.peers)
      {
        if (Nodes.TryGetValue(node, out var nodeProperties) && nodeProperties.Policies.ContainsKey(name)) continue;
        WarnLine($"Found edge {(node, name)} with no corresponding edge {(name, node)}");
        errors = true;
      }
    }

    foreach (var (node, properties) in Nodes)
    foreach (var neighbor in properties.Policies.Keys)
    {
      // check for directed edges with no corresponding back edge
      // we have to check both external and internal edges
      if ((Nodes.TryGetValue(neighbor, out var neighborProperties) && neighborProperties.Policies.ContainsKey(node))
          || Externals.Exists(ext => ext.Name == neighbor && ext.peers.Contains(node))) continue;
      WarnLine($"Found edge {(neighbor, node)} with no corresponding edge {(node, neighbor)}!");
      errors = true;
    }

    if (errors)
      throw new Exception(
        "I am giving up because there are validation errors in your Angler file. Correct the errors and rerun Timepiece.");
    Console.WriteLine("Validation passed, no errors detected!");
  }

  private static void Warn(string s)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(s);
    Console.ResetColor();
  }

  private static void WarnLine(string s)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(s);
    Console.ResetColor();
  }

  /// <summary>
  ///   Return a function composing the export and import policies to transfer a route along an edge.
  ///   The routing behavior is as follows:
  ///   <list type="number">
  ///     <item>If the route's result does not have a value, send it as-is (skip the policies -- it will be dropped).</item>
  ///     <item>Otherwise, apply the export policy with a fresh result to produce an exported route.</item>
  ///     <item>
  ///       If the exported route was rejected (i.e. its result does not have a value), send it as-is
  ///       (skip the import policy -- it will be dropped).
  ///     </item>
  ///     <item>
  ///       Otherwise, if the exported route was external (per the <paramref name="external" /> parameter),
  ///       increment the path length (if internal, leave the path length as-is). In either case, apply the import
  ///       policy with a fresh result.
  ///     </item>
  ///   </list>
  /// </summary>
  /// <param name="export">The export routing policy as a function over a <c>Zen{RouteEnvironment}</c>.</param>
  /// <param name="import">The import routing policy as a function over a <c>Zen{RouteEnvironment}</c>.</param>
  /// <param name="external">Whether or not the transfer is for an inter-AS edge.</param>
  /// <returns>A transfer function over a <c>Zen{RouteEnvironment}</c>.</returns>
  private static Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> Transfer(
    Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> export,
    Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> import,
    bool external)
  {
    return r =>
    {
      // always reset the result when first calling the export
      var exported = export(r.ResetResultControlFlow());
      // we want to make sure to retain the value that was exported, but reset Exit/Returned/Fallthrough
      var importedRoute = exported.ResetResultControlFlow();
      // if the edge is external, increment the AS path length
      if (external) importedRoute = importedRoute.IncrementAsPathLength(BigInteger.One);
      // only import the route if its result value is true; otherwise, leave it as false (which will cause it to be ignored)
      // and again reset the result
      return Zen.If(r.GetResultValue(),
        Zen.If(exported.GetResultValue(), import(importedRoute), exported), r);
    };
  }

  public (Digraph<string>, Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>)
    TopologyAndTransfer(bool trackTerms = false)
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, ImmutableSortedSet<string>>();
    var exportFunctions = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    var importFunctions = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();

    foreach (var externalPeer in Externals)
    {
      edges[externalPeer.Name] = externalPeer.peers.ToImmutableSortedSet();
      foreach (var networkNode in externalPeer.peers)
      {
        // set import and export as the identity for external peers
        // (we assume they could do anything)
        importFunctions[(networkNode, externalPeer.Name)] = e => e;
        exportFunctions[(externalPeer.Name, networkNode)] = e => e;
      }
    }

    foreach (var (node, props) in Nodes)
    {
      var details = props.CreateNode(RouteEnvironmentExtensions.ReturnAccept, RouteEnvironmentExtensions.ReturnAccept,
        trackTerms);
      // add an edge between each node and its neighbor
      foreach (var nbr in details.imports.Keys.Union(details.exports.Keys))
      {
        edges.TryAdd(node, ImmutableSortedSet<string>.Empty);
        edges[node] = edges[node].Add(nbr);
      }

      edges[node] = details.imports.Keys.Union(details.exports.Keys).ToImmutableSortedSet();
      foreach (var (nbr, fn) in details.exports) exportFunctions[(node, nbr)] = fn;

      foreach (var (nbr, fn) in details.imports) importFunctions[(nbr, node)] = fn;
    }

    var transferFunction = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    foreach (var (edge, export) in exportFunctions)
    {
      // compose the export and import and evaluate on a fresh state
      if (!importFunctions.TryGetValue(edge, out var import))
        throw new ArgumentException($"Corresponding import function absent for edge {edge}!");
      // identify an edge as inter-AS/external if the source or sink is an external peer, or if the AS numbers differ
      var external = Externals.Any(e => e.Name == edge.Item1 || e.Name == edge.Item2) ||
                     (Nodes.TryGetValue(edge.Item1, out var src) && Nodes.TryGetValue(edge.Item2, out var snk) &&
                      src.Asn != snk.Asn);
      transferFunction.Add(edge, Transfer(export, import, external));
    }

    var topology = new Digraph<string>(edges);

    return (topology, transferFunction);
  }
}

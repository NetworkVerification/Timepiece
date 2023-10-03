using System.Collections.Immutable;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Timepiece.Angler.Ast;
using Timepiece.Angler.Ast.AstExpr;
using Timepiece.Angler.Ast.AstFunction;
using Timepiece.Angler.Ast.AstStmt;
using ZenLib;

namespace Timepiece.Angler;

/// <summary>
/// Representation of the routing policies of all nodes in an Angler-produced network.
/// </summary>
public class AnglerNetwork
{
  /// <summary>
  ///   Default import behavior for a route.
  ///   Set the route as accepted and returned.
  /// </summary>
  protected static readonly AstFunction<RouteEnvironment> DefaultImport = new("env", new[]
  {
    new Assign("env", new WithField(
      new Var("env"), "Result", AstEnvironment.ResultToRecord(new RouteResult
      {
        Returned = true,
        Value = true
      })))
  });

  /// <summary>
  ///   Default export behavior for a route.
  ///   Set the route as accepted and returned.
  /// </summary>
  protected static readonly AstFunction<RouteEnvironment> DefaultExport = new("env", new Statement[]
  {
    new Assign("env",
      new WithField(new Var("env"),
        // update the result to have returned true
        "Result", AstEnvironment.ResultToRecord(new RouteResult
        {
          Returned = true,
          Value = true
        })))
  });

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
  /// Check the network for possible topology errors.
  /// The network must have an edge to and from every node.
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
    {
      foreach (var neighbor in properties.Policies.Keys)
      {
        // check for directed edges with no corresponding back edge
        // we have to check both external and internal edges
        if ((Nodes.TryGetValue(neighbor, out var neighborProperties) && neighborProperties.Policies.ContainsKey(node))
            || Externals.Exists(ext => ext.Name == neighbor && ext.peers.Contains(node))) continue;
        WarnLine($"Found edge {(neighbor, node)} with no corresponding edge {(node, neighbor)}!");
        errors = true;
      }
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

  public (Digraph<string>, Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>)
    TopologyAndTransfer()
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, ImmutableSortedSet<string>>();
    var exportFunctions = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    var importFunctions = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();

    foreach (var nbr in Externals)
    {
      edges[nbr.Name] = nbr.peers.ToImmutableSortedSet();
      foreach (var peer in nbr.peers)
      {
        // set import and export as the identity for external peers
        // (we assume they could do anything)
        importFunctions[(peer, nbr.Name)] = e => e;
        exportFunctions[(nbr.Name, peer)] = e => e;
      }
    }

    foreach (var (node, props) in Nodes)
    {
      var details = props.CreateNode(DefaultExport, DefaultImport);
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
      // compose the export and import and evaluate on a fresh state
      // NOTE: assumes that every export edge has a corresponding import edge (i.e. the graph is undirected)
      transferFunction.Add(edge, r =>
      {
        // always reset the result when first calling the export
        // TODO: seems like maybe we want to pay attention to what r.GetResultValue() was previously before exporting:
        // if it's false then the node has "no" route so we shouldn't do anything
        var exported = export(r.WithResult(new RouteResult()));
        var importedRoute = exported.WithResult(new RouteResult());
        // if the edge is external, increment the AS path length
        if (Externals.Select(e => e.Name).Contains(edge.Item1))
          importedRoute = importedRoute.IncrementAsPathLength(BigInteger.One);
        // only import the route if its result value is true; otherwise, leave it as false (which will cause it to be ignored)
        // and again reset the result
        return Zen.If(r.GetResultValue(), Zen.If(exported.GetResultValue(),
          importFunctions[edge](importedRoute), exported), new RouteEnvironment());
      });

    var topology = new Digraph<string>(edges);

    return (topology, transferFunction);
  }

  public static ISerializationBinder Binder()
  {
    return new AstSerializationBinder();
  }
}

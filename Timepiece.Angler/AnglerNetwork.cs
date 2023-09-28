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
  ///   If external is true, increment the path length.
  ///   In either case, set the route as accepted and returned.
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

    // using Evaluate() to convert AST elements into functions over Zen values is likely to be a bit slow
    // we hence want to try and do as much of this as possible up front
    // this also means inlining constants and evaluating and inlining predicates where possible
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
        var exported = export(r.WithResult(new RouteResult()));
        var importedRoute = exported.WithResult(new RouteResult());
        // if the edge is external, increment the AS path length
        if (Externals.Select(e => $"{e.ip}").Contains(edge.Item1))
          importedRoute = importedRoute.IncrementAsPathLength(BigInteger.One);
        // only import the route if its result value is true; otherwise, leave it as false (which will cause it to be ignored)
        // and again reset the result
        return Zen.If(exported.GetResult().GetValue(),
          importFunctions[edge](importedRoute), exported);
      });

    var topology = new Digraph<string>(edges);

    return (topology, transferFunction);
  }

  public static ISerializationBinder Binder()
  {
    return new AstSerializationBinder();
  }
}

using System.Net;
using System.Numerics;
using Gardener.AstExpr;
using Gardener.AstFunction;
using Gardener.AstStmt;
using Karesansui;
using Karesansui.Networks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZenLib;

namespace Gardener;

using Route = Pair<bool, BatfishBgpRoute>;
using AstZenFunction = Func<Zen<Pair<bool, BatfishBgpRoute>>, Zen<Pair<bool, BatfishBgpRoute>>>;
using AstZenConstraint = Func<Zen<Pair<bool, BatfishBgpRoute>>, Zen<bool>>;
using AstZenTemporalConstraint = Func<Zen<Pair<bool, BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>;

public class Ast
{
  /// <summary>
  /// The nodes of the network with their associated policies.
  /// </summary>
  public Dictionary<string, NodeProperties<Route>> Nodes { get; set; }

  /// <summary>
  /// Additional function declarations.
  /// </summary>
  public Dictionary<string, AstFunction<Route>> Declarations { get; set; }

  /// <summary>
  /// Additional constant declarations.
  /// </summary>
  public Dictionary<string, JObject> Constants { get; set; }

  /// <summary>
  /// Symbolic expressions.
  /// </summary>
  public Dictionary<string, JObject> Symbolics { get; set; }

  /// <summary>
  /// Assertions over routes.
  /// </summary>
  public Dictionary<string, AstPredicate<Route>> Assertions { get; set; }


  /// <summary>
  /// Temporal invariants over routes.
  /// </summary>
  public Dictionary<string, AstPredicate<Pair<Route, BigInteger>>> Invariants { get; set; }

  [System.Text.Json.Serialization.JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<Route>> nodes,
    Dictionary<string, AstFunction<Route>> declarations, Dictionary<string, JObject> symbolics,
    Dictionary<string, JObject> constants, Dictionary<string, AstPredicate<Route>> assertions,
    Dictionary<string, AstPredicate<Pair<Route, BigInteger>>> invariants)
  {
    Nodes = nodes;
    Declarations = declarations;
    Symbolics = symbolics;
    Constants = constants;
    Assertions = assertions;
    Invariants = invariants;
    DisambiguateVariableNames();
  }

  /// <summary>
  /// Make the arguments to all AstFunctions unique.
  /// </summary>
  private void DisambiguateVariableNames()
  {
    foreach (var function in Declarations.Values)
    {
      function.Rename(function.Arg, $"${function.Arg}~{VarCounter.Request()}");
      Console.WriteLine($"New function arg: {function.Arg}");
    }
  }

  public static JsonSerializer Serializer()
  {
    return new JsonSerializer
    {
      TypeNameHandling = TypeNameHandling.All,
      SerializationBinder = new AstSerializationBinder<BatfishBgpRoute, Route>()
    };
  }

  // default export behavior for a route, always used
  public static AstFunction<Route> DefaultExport()
  {
    return new AstFunction<Route>("arg",
      new Return<Route>(
        new PairExpr<bool, BatfishBgpRoute, Route>(
          new First<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
          new WithField<BatfishBgpRoute, int, Route>(new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
            "AsPathLength",
            new Plus<int, Route>(
              new GetField<BatfishBgpRoute, int, Route>(new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
                "AsPathLength"), new ConstantExpr<int, Route>(1))))));
  }

  public Network<Route, TS> ToNetwork<TS>(IPAddress? destination)
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, List<string>>();
    var importFunctions = new Dictionary<(string, string), AstFunction<Route>>();
    var exportFunctions = new Dictionary<(string, string), AstFunction<Route>>();
    var initFunction = new Dictionary<string, Zen<Route>>();
    var monolithicAssertions = new Dictionary<string, AstZenConstraint>();
    var annotations = new Dictionary<string, AstZenTemporalConstraint>();

    foreach (var (node, props) in Nodes)
    {
      if (!edges.ContainsKey(node))
      {
        edges.Add(node, new List<string>());
      }

      // init
      initFunction[node] = Pair.Create<bool, BatfishBgpRoute>(
        props.Prefixes.Any(range => range.Contains(destination)),
        new BatfishBgpRoute());

      // assert
      if (props.Assert is null)
      {
        monolithicAssertions[node] = _ => true;
      }
      else
      {
        var assert = Assertions[props.Assert];
        monolithicAssertions[node] = assert.Evaluate(new State<Route>());
      }

      // invariant
      if (props.Invariant is null)
      {
        annotations[node] = (_, _) => true;
      }
      else
      {
        var inv = Invariants[props.Invariant];
        var fn = inv.Evaluate(new State<Pair<Route, BigInteger>>());
        annotations[node] = (r, t) => fn(Pair.Create(r, t));
      }

      // transfer
      foreach (var (neighbor, policies) in props.Policies)
      {
        edges[node].Add(neighbor);
        var fwdEdge = (node, neighbor);
        var bwdEdge = (neighbor, node);
        // get each declaration and cast it to an AstFunc from route to route
        var expFuncs = policies.Export.Select(policyName => Declarations[policyName]);
        var impFuncs = policies.Import.Select(policyName => Declarations[policyName]);
        var export = AstFunction<Route>.Compose(expFuncs, DefaultExport());
        var import = AstFunction<Route>.Compose(impFuncs, AstFunction<Route>.Identity());
        // set the policies
        exportFunctions[fwdEdge] = export;
        importFunctions[bwdEdge] = import;
      }
    }

    var transferFunction = new Dictionary<(string, string), AstZenFunction>();
    foreach (var (edge, export) in exportFunctions)
    {
      // compose the export and import and evaluate on a fresh state
      // NOTE: assumes that every export edge has a corresponding import edge (i.e. the graph is undirected)
      transferFunction.Add(edge, export.Compose(importFunctions[edge]).Evaluate(new State<Route>()));
    }

    var topology = new Topology(edges);

    return new Network<Route, TS>(topology,
      transferFunction,
      BatfishBgpRouteExtensions.MinPair,
      initFunction,
      annotations,
      topology.ForAllNodes(n => Lang.Finally(new BigInteger(topology.NEdges), monolithicAssertions[n])),
      monolithicAssertions, Array.Empty<SymbolicValue<TS>>());
  }
}

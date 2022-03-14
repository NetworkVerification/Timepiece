using System.Diagnostics.Metrics;
using System.Net;
using System.Numerics;
using Karesansui;
using Karesansui.Networks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZenLib;

namespace Gardener;

using AstZenFunction = Func<Zen<BatfishBgpRoute>, Zen<BatfishBgpRoute>>;
using AstZenConstraint = Func<Zen<BatfishBgpRoute>, Zen<bool>>;
using AstZenTemporalConstraint = Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>;

public class Ast
{
  /// <summary>
  /// The nodes of the network with their associated policies.
  /// </summary>
  public Dictionary<string, NodeProperties<BatfishBgpRoute>> Nodes { get; set; }

  /// <summary>
  /// Additional function declarations.
  /// </summary>
  public Dictionary<string, AstFunction<BatfishBgpRoute>> Declarations { get; set; }

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
  public Dictionary<string, AstPredicate<BatfishBgpRoute>> Assertions { get; set; }

  [System.Text.Json.Serialization.JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<BatfishBgpRoute>> nodes,
    Dictionary<string, AstFunction<BatfishBgpRoute>> declarations, Dictionary<string, JObject> symbolics,
    Dictionary<string, JObject> constants, Dictionary<string, AstPredicate<BatfishBgpRoute>> assertions)
  {
    Nodes = nodes;
    Declarations = declarations;
    // make the declaration functions' arguments unique
    foreach (var (name, function) in Declarations)
    {
      function.Rename(function.Arg, $"${function.Arg}~{VarCounter.Request()}");
    }
    Symbolics = symbolics;
    Constants = constants;
    Assertions = assertions;
  }

  public static JsonSerializer Serializer()
  {
    return new JsonSerializer
    {
      TypeNameHandling = TypeNameHandling.All,
      SerializationBinder = new AstBinder()
    };
  }

  public Network<BatfishBgpRoute, TS> ToNetwork<TS>(IPAddress? destination)
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, List<string>>();
    var importFunctions = new Dictionary<(string, string), AstFunction<BatfishBgpRoute>>();
    var exportFunctions = new Dictionary<(string, string), AstFunction<BatfishBgpRoute>>();
    var initFunction = new Dictionary<string, Zen<BatfishBgpRoute>>();
    var monolithicAssertions = new Dictionary<string, AstZenConstraint>();
    var annotations = new Dictionary<string, AstZenTemporalConstraint>();

    foreach (var (node, props) in Nodes)
    {
      if (!edges.ContainsKey(node))
      {
        edges.Add(node, new List<string>());
      }

      // init
      if (props.Prefixes.Any(range => range.Contains(destination)))
      {
        initFunction[node] = Zen.Constant(new BatfishBgpRoute()).Valid(true);
      }
      else
      {
        initFunction[node] = new BatfishBgpRoute();
      }

      // assert
      if (props.Assert is null)
      {
        monolithicAssertions[node] = _ => true;
      }
      else
      {
        var assert = Assertions[props.Assert];
        monolithicAssertions[node] = assert.Evaluate(new State<BatfishBgpRoute>());
      }

      // invariant
      if (props.Invariant is null)
      {
        annotations[node] = (_, _) => true;
      }
      else
      {
        var fn =
          props.Invariant.Evaluate(new State<Pair<BatfishBgpRoute, BigInteger>>());
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
        var export = AstFunction<BatfishBgpRoute>.Compose(expFuncs);
        var import = AstFunction<BatfishBgpRoute>.Compose(impFuncs);
        // set the policies if they are missing
        exportFunctions[fwdEdge] = export;
        importFunctions[bwdEdge] = import;
      }
    }

    var transferFunction = new Dictionary<(string, string), AstZenFunction>();
    foreach (var (edge, export) in exportFunctions)
    {
      // compose the export and import and evaluate on a fresh state
      // NOTE: assumes that every export edge has a corresponding import edge (i.e. the graph is undirected)
      transferFunction.Add(edge, export.Compose(importFunctions[edge]).Evaluate(new State<BatfishBgpRoute>()));
    }

    var topology = new Topology(edges);

    return new Network<BatfishBgpRoute, TS>(topology,
      transferFunction,
      BatfishBgpRouteExtensions.Min,
      initFunction,
      annotations,
      topology.ForAllNodes(n => Lang.Finally(new BigInteger(topology.NEdges), monolithicAssertions[n])),
      monolithicAssertions, Array.Empty<SymbolicValue<TS>>());
  }
}

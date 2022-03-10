using System.Numerics;
using System.Text.Json.Serialization;
using Karesansui;
using Karesansui.Networks;
using ZenLib;

namespace Gardener;

public class Ast
{
  /// <summary>
  /// The nodes of the network with their associated policies.
  /// </summary>
  public Dictionary<string, NodeProperties<BatfishBgpRoute>> Nodes { get; set; }

  /// <summary>
  /// Additional function declarations.
  /// </summary>
  public Dictionary<string, object> Declarations { get; set; }

  /// <summary>
  /// Additional constant declarations.
  /// </summary>
  public Dictionary<string, object> Constants { get; set; }

  /// <summary>
  /// Symbolic expressions.
  /// </summary>
  public Dictionary<string, object> Symbolics { get; set; }

  /// <summary>
  /// Assertions over nodes.
  /// </summary>
  public Dictionary<string, object> Assertions { get; set; }

  [JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<BatfishBgpRoute>> nodes,
    Dictionary<string, object> declarations, Dictionary<string, object> symbolics,
    Dictionary<string, object> constants, Dictionary<string, object> assertions)
  {
    Nodes = nodes;
    Declarations = declarations;
    Symbolics = symbolics;
    Constants = constants;
    Assertions = assertions;
  }

  public Network<BatfishBgpRoute, TS> ToNetwork<TS>()
  {
    var edges = new Dictionary<string, List<string>>();
    var transferAstFunctions =
      new Dictionary<(string, string), (AstFunc<BatfishBgpRoute, BatfishBgpRoute>,
        AstFunc<BatfishBgpRoute, BatfishBgpRoute>)>();
    // TODO: assign a route for each node
    var initFunction = new Dictionary<string, Zen<BatfishBgpRoute>>();
    foreach (var (node, props) in Nodes)
    {
      if (!edges.ContainsKey(node))
      {
        edges.Add(node, new List<string>());
      }

      foreach (var (neighbor, policies) in props.Policies)
      {
        edges[node].Add(neighbor);
        var fwdEdge = (node, neighbor);
        var bwdEdge = (neighbor, node);
        var exportFunctions = policies.Export.Select(policyName =>
          (AstFunc<BatfishBgpRoute, BatfishBgpRoute>) Declarations[policyName]);
        var importFunctions= policies.Import.Select(policyName =>
          (AstFunc<BatfishBgpRoute, BatfishBgpRoute>) Declarations[policyName]);
        var export = AstFuncExtensions.Compose(exportFunctions);
        var import = AstFuncExtensions.Compose(importFunctions);
        // set the policies if they are missing
        if (transferAstFunctions.TryGetValue(fwdEdge, out var policy))
        {
          transferAstFunctions[fwdEdge] = (export, policy.Item2);
        }

        if (transferAstFunctions.TryGetValue(bwdEdge, out policy))
        {
          transferAstFunctions[bwdEdge] = (policy.Item1, import);
        }
      }
    }

    var transferFunction = new Dictionary<(string, string), Func<Zen<BatfishBgpRoute>, Zen<BatfishBgpRoute>>>();
    foreach (var (edge, (export, import)) in transferAstFunctions)
    {
      // compose the export and import and evaluate on a fresh state
      transferFunction.Add(edge, export.Compose(import).Evaluate(new State()));
    }

    var topology = new Topology(edges);

    // foreach (var (node, stmt) in InitFunction)
    // {
    // var returned = stmt.Evaluate(new State()).Return;
    // if (returned is null)
    // {
    // throw new ArgumentException($"No value returned by initialFunction for node {node}");
    // }
    // init.Add(node, (Zen<BatfishBgpRoute>) returned);
    // }
    // TODO: set properties, symbolics and annotations
    return new Network<BatfishBgpRoute, TS>(topology,
      transferFunction,
      BatfishBgpRouteExtensions.Min,
      initFunction,
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<bool>>>(), Array.Empty<SymbolicValue<TS>>());
  }
}

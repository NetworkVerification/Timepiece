using System.Numerics;
using System.Text.Json.Serialization;
using Karesansui;
using Karesansui.Networks;
using ZenLib;

namespace Gardener;

public class AstAlt
{
  public Dictionary<string, NodeProperties<BatfishBgpRoute>> Nodes { get; set; }

  public Dictionary<string, AstFunc<object, object>> Declarations { get; set; }

  // TODO: use symbolics
  public List<(string, Expr<bool>)> Symbolics { get; set; }

  [JsonConstructor]
  public AstAlt(Dictionary<string, NodeProperties<BatfishBgpRoute>> nodes,
    Dictionary<string, AstFunc<object, object>> declarations, List<(string, Expr<bool>)> symbolics)
  {
    Nodes = nodes;
    Declarations = declarations;
    Symbolics = symbolics;
  }

  public Network<BatfishBgpRoute, TS> ToNetwork<TS>()
  {
    var edges = new Dictionary<string, List<string>>();
    var transferAstFuncs =
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
        var export = AstFunc<BatfishBgpRoute, BatfishBgpRoute>.Compose(policies.Export);
        var import = AstFunc<BatfishBgpRoute, BatfishBgpRoute>.Compose(policies.Import);
        // set the policies if they are missing
        if (transferAstFuncs.TryGetValue(fwdEdge, out var policy))
        {
          transferAstFuncs[fwdEdge] = (export, policy.Item2);
        }

        if (transferAstFuncs.TryGetValue(bwdEdge, out policy))
        {
          transferAstFuncs[bwdEdge] = (policy.Item1, import);
        }
      }
    }

    var transferFunction = new Dictionary<(string, string), Func<Zen<BatfishBgpRoute>, Zen<BatfishBgpRoute>>>();
    foreach (var (edge, (export, import)) in transferAstFuncs)
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
    return new Network<BatfishBgpRoute, TS>(topology,
      transferFunction,
      BatfishBgpRouteExtensions.Min,
      initFunction,
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<bool>>>(), Array.Empty<SymbolicValue<TS>>());
  }
}

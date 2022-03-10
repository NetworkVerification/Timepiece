using System.Numerics;
using Karesansui;
using Karesansui.Networks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
  public Dictionary<string, JObject> Declarations { get; set; }

  /// <summary>
  /// Additional constant declarations.
  /// </summary>
  public Dictionary<string, JObject> Constants { get; set; }

  /// <summary>
  /// Symbolic expressions.
  /// </summary>
  public Dictionary<string, JObject> Symbolics { get; set; }

  /// <summary>
  /// Assertions over nodes.
  /// </summary>
  public Dictionary<string, JObject> Assertions { get; set; }

  [System.Text.Json.Serialization.JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<BatfishBgpRoute>> nodes,
    Dictionary<string, JObject> declarations, Dictionary<string, JObject> symbolics,
    Dictionary<string, JObject> constants, Dictionary<string, JObject> assertions)
  {
    Nodes = nodes;
    Declarations = declarations;
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

  public Network<BatfishBgpRoute, TS> ToNetwork<TS>()
  {
    var serializer = Serializer();
    var edges = new Dictionary<string, List<string>>();
    var transferAstFunctions =
      new Dictionary<(string, string), (AstFunc<BatfishBgpRoute>,
        AstFunc<BatfishBgpRoute>)>();
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
        // get each declaration and cast it to an AstFunc from route to route
        var exportFunctions = policies.Export.Select(policyName =>
          Declarations[policyName].ToObject<AstFunc<BatfishBgpRoute>>(serializer)!);
        var importFunctions= policies.Import.Select(policyName =>
          Declarations[policyName].ToObject<AstFunc<BatfishBgpRoute>>(serializer)!);
        var export = AstFunc<BatfishBgpRoute>.Compose(exportFunctions);
        var import = AstFunc<BatfishBgpRoute>.Compose(importFunctions);
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
      transferFunction.Add(edge, export.Compose(import).Evaluate(new State<BatfishBgpRoute>()));
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

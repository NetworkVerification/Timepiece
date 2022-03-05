using System.Numerics;
using System.Text.Json.Serialization;
using Karesansui;
using Karesansui.Networks;
using ZenLib;

namespace Gardener;

public class Ast
{
  public Topology Topology { get; set; }

  [JsonPropertyName("transfer")]
  public Dictionary<(string, string), AstFunc<BatfishBgpRoute, BatfishBgpRoute>> TransferFunction { get; set; }

  [JsonPropertyName("init")]
  public Dictionary<string, Statement> InitFunction { get; set; }

  [JsonPropertyName("declarations")]
  public Dictionary<string, AstFunc<object, object>> Declarations { get; set; }

  // TODO: use symbolics
  public List<(string, Expr<bool>)> Symbolics { get; set; }

  [JsonConstructor]
  public Ast(Topology topology, Dictionary<(string, string), AstFunc<BatfishBgpRoute, BatfishBgpRoute>> transferFunction,
    Dictionary<string, Statement> initFunction, Dictionary<string, AstFunc<object, object>> declarations, List<(string, Expr<bool>)> symbolics)
  {
    Topology = topology;
    TransferFunction = transferFunction;
    InitFunction = initFunction;
    Declarations = declarations;
    Symbolics = symbolics;
  }

  public Network<BatfishBgpRoute, TS> ToNetwork<TS>()
  {
    var transfer = new Dictionary<(string, string), Func<Zen<BatfishBgpRoute>, Zen<BatfishBgpRoute>>>();
    foreach (var (edge, astFunc) in TransferFunction)
    {
      transfer.Add(edge, astFunc.Evaluate(new State()));
    }

    var init = new Dictionary<string, Zen<BatfishBgpRoute>>();
    foreach (var (node, stmt) in InitFunction)
    {
      var returned = stmt.Evaluate(new State()).Return;
      if (returned is null)
      {
        throw new ArgumentException($"No value returned by initialFunction for node {node}");
      }
      init.Add(node, (Zen<BatfishBgpRoute>) returned);
    }
    return new Network<BatfishBgpRoute, TS>(Topology,
      transfer,
      BatfishBgpRouteExtensions.Min,
      init,
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<bool>>>(), Array.Empty<SymbolicValue<TS>>());
  }
}

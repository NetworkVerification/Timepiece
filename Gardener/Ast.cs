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
  public Dictionary<(string, string), AstFunc> TransferFunction { get; set; }

  [JsonPropertyName("merge")]
  public AstFunc MergeFunction { get; set; }

  [JsonPropertyName("init")]
  public Dictionary<string, Expr> InitFunction { get; set; }

  [JsonPropertyName("declarations")]
  public Dictionary<string, AstFunc> Declarations { get; set; }

  [JsonConstructor]
  public Ast(Topology topology, Dictionary<(string, string), AstFunc> transferFunction, AstFunc mergeFunction,
    Dictionary<string, Expr> initFunction, Dictionary<string, AstFunc> declarations)
  {
    Topology = topology;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitFunction = initFunction;
    Declarations = declarations;
  }

  public Network<T, TS> ToNetwork<T, TS>()
  {
    var transfer = new Dictionary<(string, string), Func<Zen<T>, Zen<T>>>();
    foreach (var (edge, astFunc) in TransferFunction)
    {
      transfer.Add(edge, astFunc.ToZenUnary());
    }
    return new Network<T, TS>(Topology,
      transfer,
      MergeFunction.ToZenBinary(),
      InitFunction.Select(p => (p.Key, p.Value.ToZen<T>())).ToDictionary(p => p.Item1, p => p.Item2),
      new Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<T>, Zen<bool>>>(), Array.Empty<SymbolicValue<TS>>());
  }
}

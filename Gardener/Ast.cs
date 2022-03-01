using System.Numerics;
using Karesansui;
using Karesansui.Networks;
using ZenLib;

namespace Gardener;

public class Ast
{
  private Topology Topology { get; }

  private Dictionary<(string, string), AstFunc> TransferFunction { get; init; }

  private AstFunc MergeFunction { get; init; }

  private Dictionary<string, Expr> InitFunction { get; init; }

  public Ast(Topology topology, Dictionary<(string, string), AstFunc> transferFunction, AstFunc mergeFunction,
    Dictionary<string, Expr> initFunction)
  {
    Topology = topology;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitFunction = initFunction;
  }

  public Network<dynamic, dynamic> ToNetwork()
  {
    return new Network<dynamic, dynamic>(Topology,
      TransferFunction.Select(p => (p.Key, p.Value.ToZenUnary())).ToDictionary(p => p.Item1, p => p.Item2),
      MergeFunction.ToZenBinary(),
      InitFunction.Select(p => (p.Key, p.Value.ToZen<dynamic>())).ToDictionary(p => p.Item1, p => p.Item2),
      new Dictionary<string, Func<Zen<dynamic>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<dynamic>, Zen<BigInteger>, Zen<bool>>>(),
      new Dictionary<string, Func<Zen<dynamic>, Zen<bool>>>(), Array.Empty<SymbolicValue<object>>());
  }
}

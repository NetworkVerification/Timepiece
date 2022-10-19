using System.Numerics;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

public class Globally : AstTemporalOperator
{
  public Globally(string p)
  {
    Predicate = p;
  }

  public string Predicate { get; }

  public override Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate> getter,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations)
  {
    var f = getter(Predicate).Evaluate(new AstEnvironment(declarations));
    return Lang.Globally(f);
  }

  public override string ToString()
  {
    return $"Globally({Predicate})";
  }
}

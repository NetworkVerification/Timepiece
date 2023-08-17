using System.Numerics;
using ZenLib;

namespace Timepiece.Angler.Ast.AstFunction;

public class Finally : AstTemporalOperator
{
  public Finally(BigInteger time, string then)
  {
    Time = time;
    Then = then;
  }

  public BigInteger Time { get; set; }

  public string Then { get; set; }

  public override Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>> Evaluate(
    Func<string, AstPredicate> getter,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations)
  {
    var f = getter(Then).Evaluate(new AstEnvironment(declarations));
    return Lang.Finally(Time, f);
  }

  public override string ToString()
  {
    return $"Finally({Time}, {Then})";
  }
}

using System.Numerics;
using ZenLib;

namespace Timekeeper.Json.AstFunction;

public class Finally<T> : AstTemporalOperator<T>
{
  public BigInteger Time { get; set; }

  public string Then { get; set; }

  public Finally(BigInteger time, string then)
  {
    Time = time;
    Then = then;
  }

  public override Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter)
  {
    var f = getter(Then).Evaluate(new AstState<T>());
    return Lang.Finally(Time, f);
  }

  public override string ToString()
  {
    return $"Finally({Time}, {Then})";
  }
}

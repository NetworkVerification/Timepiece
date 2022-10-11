using System.Numerics;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

public class Finally<T> : AstTemporalOperator<T>
{
  public Finally(BigInteger time, string then)
  {
    Time = time;
    Then = then;
  }

  public BigInteger Time { get; set; }

  public string Then { get; set; }

  public override Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter)
  {
    var f = getter(Then).Evaluate(new AstEnvironment());
    return Lang.Finally(Time, f);
  }

  public override string ToString()
  {
    return $"Finally({Time}, {Then})";
  }
}

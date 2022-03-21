using System.Numerics;
using Karesansui;
using ZenLib;

namespace Gardener.AstFunction;

public class Until<T> : AstTemporalOperator<T>
{
  public BigInteger Time { get; set; }
  public string Before { get; set; }
  public string After { get; set; }

  public Until(BigInteger t, string before, string after)
  {
    Time = t;
    Before = before;
    After = after;
  }

  public override Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter)
  {
    var beforeF = getter(Before).Evaluate(new State<T>());
    var afterF = getter(After).Evaluate(new State<T>());
    return Lang.Until(Time, beforeF, afterF);
  }
}

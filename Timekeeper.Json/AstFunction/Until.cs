using System.Numerics;
using Timekeeper;
using Newtonsoft.Json;
using ZenLib;

namespace Gardener.AstFunction;

public class Until<T> : AstTemporalOperator<T>
{
  public BigInteger Time { get; set; }
  public string Before { get; set; }
  public string After { get; set; }

  [JsonConstructor]
  public Until(BigInteger time, string before, string after)
  {
    Time = time;
    Before = before;
    After = after;
  }

  public override Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter)
  {
    var beforeF = getter(Before).Evaluate(new AstState<T>());
    var afterF = getter(After).Evaluate(new AstState<T>());
    return Lang.Until(Time, beforeF, afterF);
  }

  public override string ToString()
  {
    return $"Until({Time}, {Before}, {After})";
  }
}

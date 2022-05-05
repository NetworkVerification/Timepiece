using System.Numerics;
using Newtonsoft.Json;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstFunction;

public class Until<T> : AstTemporalOperator<T>
{
  [JsonConstructor]
  public Until(BigInteger time, string before, string after)
  {
    Time = time;
    Before = before;
    After = after;
  }

  public BigInteger Time { get; set; }
  public string Before { get; set; }
  public string After { get; set; }

  public override Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter)
  {
    var beforeF = getter(Before).Evaluate();
    var afterF = getter(After).Evaluate();
    return Lang.Until(Time, beforeF, afterF);
  }

  public override string ToString()
  {
    return $"Until({Time}, {Before}, {After})";
  }
}

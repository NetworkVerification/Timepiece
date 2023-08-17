using System.Numerics;
using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.Ast.AstFunction;

public class Until : AstTemporalOperator
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

  public override Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate> getter,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations)
  {
    var beforeF = getter(Before).Evaluate(new AstEnvironment(declarations));
    var afterF = getter(After).Evaluate(new AstEnvironment(declarations));
    return Lang.Until(Time, beforeF, afterF);
  }

  public override string ToString()
  {
    return $"Until({Time}, {Before}, {After})";
  }
}

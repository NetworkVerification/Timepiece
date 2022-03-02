using ZenLib;

namespace Gardener;

public class Havoc : Expr
{
  public override Zen<T> ToZen<T>()
  {
    throw new NotImplementedException();
  }

  public override Func<dynamic, Zen<bool>> Evaluate(State state)
  {
    return _ => Zen.Symbolic<bool>();
  }
}

using ZenLib;

namespace Gardener;

public class Havoc : Expr
{
  public override Zen<T> ToZen<T>()
  {
    throw new NotImplementedException();
  }

  public override Zen<bool> Evaluate(State state)
  {
    return Zen.Symbolic<bool>();
  }
}

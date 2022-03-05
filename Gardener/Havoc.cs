using ZenLib;

namespace Gardener;

public class Havoc : Expr<bool>
{
  public override Zen<bool> ToZen()
  {
    return Zen.Symbolic<bool>();
  }

  public override Func<Zen<TInput>, Zen<bool>> Evaluate<TInput>(State state)
  {
    return _ => Zen.Symbolic<bool>();
  }
}

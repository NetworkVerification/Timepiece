using ZenLib;

namespace Gardener;

public class And : Expr<bool>
{
  private readonly Expr<bool> _e1;
  private readonly Expr<bool> _e2;

  public And(Expr<bool> e1, Expr<bool> e2)
  {
    _e1 = e1;
    _e2 = e2;
  }
  public override Zen<bool> ToZen()
  {
    return Zen.And(_e1.ToZen(), _e2.ToZen());
  }

  public override Func<Zen<TInput>, Zen<bool>> Evaluate<TInput>(State state)
  {
    return t => Zen.And(_e1.Evaluate<TInput>(state)(t), _e2.Evaluate<TInput>(state)(t));
  }
}

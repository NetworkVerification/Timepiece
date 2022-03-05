using ZenLib;

namespace Gardener;

public class Plus<TWidth, TSigned> : Expr<IntN<TWidth, TSigned>>
{
  private readonly Expr<IntN<TWidth, TSigned>> _e1;
  private readonly Expr<IntN<TWidth, TSigned>> _e2;

  public Plus(Expr<IntN<TWidth, TSigned>> e1, Expr<IntN<TWidth, TSigned>> e2)
  {
    _e1 = e1;
    _e2 = e2;
  }

  public override Zen<IntN<TWidth, TSigned>> ToZen()
  {
    throw new NotImplementedException();
  }

  public override Func<Zen<TInput>, Zen<IntN<TWidth, TSigned>>> Evaluate<TInput>(State state)
  {
    return t => Zen.Plus(_e1.Evaluate<TInput>(state)(t), _e2.Evaluate<TInput>(state)(t));
  }
}

using ZenLib;

namespace Gardener;

public class Plus<TWidth, TSigned, TState> : Expr<IntN<TWidth, TSigned>, TState>
{
  private readonly Expr<IntN<TWidth, TSigned>, TState> _e1;
  private readonly Expr<IntN<TWidth, TSigned>, TState> _e2;

  public Plus(Expr<IntN<TWidth, TSigned>, TState> e1, Expr<IntN<TWidth, TSigned>, TState> e2)
  {
    _e1 = e1;
    _e2 = e2;
  }

  public override Func<Zen<TState>, Zen<IntN<TWidth, TSigned>>> Evaluate(State<TState> state)
  {
    return t => Zen.Plus(_e1.Evaluate(state)(t), _e2.Evaluate(state)(t));
  }
}

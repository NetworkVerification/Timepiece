using ZenLib;

namespace Gardener;

public class And<TState> : Expr<bool, TState>
{
  private readonly Expr<bool, TState> _e1;
  private readonly Expr<bool, TState> _e2;

  public And(Expr<bool, TState> e1, Expr<bool, TState> e2)
  {
    _e1 = e1;
    _e2 = e2;
  }

  public override Func<Zen<TState>, Zen<bool>> Evaluate(State<TState> state)
  {
    return t => Zen.And(_e1.Evaluate(state)(t), _e2.Evaluate(state)(t));
  }
}

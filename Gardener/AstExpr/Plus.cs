using ZenLib;

namespace Gardener.AstExpr;

public class Plus<T, TState> : Expr<T, TState>
{
  private readonly Expr<T, TState> _e1;
  private readonly Expr<T, TState> _e2;

  public Plus(Expr<T, TState> e1, Expr<T, TState> e2)
  {
    _e1 = e1;
    _e2 = e2;
  }

  public override Func<Zen<TState>, Zen<T>> Evaluate(State<TState> state)
  {
    return t => Zen.Plus(_e1.Evaluate(state)(t), _e2.Evaluate(state)(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _e1.Rename(oldVar, newVar);
    _e2.Rename(oldVar, newVar);
  }
}

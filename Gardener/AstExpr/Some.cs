using ZenLib;

namespace Gardener.AstExpr;

public class Some<T, TState> : Expr<Option<T>, TState>
{
  private readonly Expr<T, TState> _value;

  public Some(Expr<T, TState> value)
  {
    _value = value;
  }

  public override Func<Zen<TState>, Zen<Option<T>>> Evaluate(State<TState> state)
  {
    return s => Option.Create(_value.Evaluate(state)(s));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _value.Rename(oldVar, newVar);
  }
}

using ZenLib;

namespace Gardener;

public class IntExpr<T, TSign, TState> : Expr<IntN<T, TSign>, TState>
{
  private readonly IntN<T, TSign> _value;

  public IntExpr(IntN<T, TSign> value)
  {
    _value = value;
  }

  public override Func<Zen<TState>, Zen<IntN<T, TSign>>> Evaluate(State<TState> state)
  {
    return _ => Zen.Constant(_value);
  }
}

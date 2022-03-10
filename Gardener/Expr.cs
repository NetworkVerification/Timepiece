using ZenLib;

namespace Gardener;

public abstract class Expr<T, TState>
{
  public abstract Func<Zen<TState>, Zen<T>> Evaluate(State<TState> state);
}

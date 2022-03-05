using ZenLib;

namespace Gardener;

public abstract class Expr<T>
{
  public abstract Zen<T> ToZen();

  public abstract Func<Zen<TInput>, Zen<T>> Evaluate<TInput>(State state);
}

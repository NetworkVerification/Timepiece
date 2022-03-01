using ZenLib;

namespace Gardener;

public abstract class Expr
{
  public abstract Zen<T> ToZen<T>();

  public abstract dynamic Evaluate();
}

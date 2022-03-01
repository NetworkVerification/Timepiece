using ZenLib;

namespace Gardener;

public abstract class Statement
{
  public abstract Func<Zen<T>,Zen<T>> ToZen<T>();
}

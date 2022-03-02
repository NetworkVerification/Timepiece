using ZenLib;

namespace Gardener;

public abstract class Statement
{
  public abstract Func<Zen<dynamic>, Zen<dynamic>> ToZen();

  public abstract State Evaluate(State state);
}

using ZenLib;

namespace Gardener;

public abstract class Statement
{
  public abstract State Evaluate(State state);
}

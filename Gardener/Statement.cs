using ZenLib;

namespace Gardener;

public abstract class Statement
{
  public abstract Func<Zen<dynamic>, Zen<dynamic>> ToZen();

  public abstract Dictionary<string, dynamic> Evaluate(Dictionary<string, dynamic> state);
  // Assign(x, e)
  //
}

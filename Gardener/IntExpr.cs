using ZenLib;

namespace Gardener;

public class IntExpr<T, TSign> : Expr<IntN<T, TSign>>
{
  private int _value;

  public IntExpr(int value)
  {
    _value = value;
  }

  public override Zen<IntN<T, TSign>> ToZen()
  {
    throw new NotImplementedException();
  }

  public override Func<Zen<TInput>, Zen<IntN<T, TSign>>> Evaluate<TInput>(State state)
  {
    throw new NotImplementedException();
  }
}

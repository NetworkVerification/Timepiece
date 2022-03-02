using ZenLib;

namespace Gardener;

public class And : Expr
{
  private readonly Expr _e1;
  private readonly Expr _e2;

  public And(Expr e1, Expr e2)
  {
    _e1 = e1;
    _e2 = e2;
  }
  public override Zen<T> ToZen<T>()
  {
    throw new NotImplementedException();
  }

  public override Func<dynamic, Zen<bool>> Evaluate(State state)
  {
    return t => _e1.Evaluate(state)(t) && _e2.Evaluate(state)(t);
  }
}

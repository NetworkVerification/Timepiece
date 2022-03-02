using ZenLib;

namespace Gardener;

public class Plus<TWidth> : Expr where TWidth: IntN<TWidth, Signed>
{
  private readonly Expr _e1;
  private readonly Expr _e2;

  public Plus(Expr e1, Expr e2)
  {
    _e1 = e1;
    _e2 = e2;
  }
  public override Zen<T> ToZen<T>()
  {
    throw new NotImplementedException();
  }

  public override Func<dynamic, Zen<TWidth>> Evaluate(State state)
  {
    return t => Zen.Plus(_e1.Evaluate(state)(t), _e2.Evaluate(state)(t));
  }
}

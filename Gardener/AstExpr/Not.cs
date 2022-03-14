using ZenLib;

namespace Gardener.AstExpr;

public class Not<T> : Expr<bool, T>
{
  private readonly Expr<bool, T> _e;

  public Not(Expr<bool, T> e)
  {
    _e = e;
  }

  public override Func<Zen<T>, Zen<bool>> Evaluate(State<T> state)
  {
    return t => Zen.Not(_e.Evaluate(state)(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _e.Rename(oldVar, newVar);
  }
}

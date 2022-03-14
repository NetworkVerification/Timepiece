using ZenLib;

namespace Gardener.AstExpr;

public class Or<T> : Expr<bool, T>
{
  private readonly Expr<bool, T> _e1;
  private readonly Expr<bool, T> _e2;

  public Or(Expr<bool, T> e1, Expr<bool, T> e2)
  {
    _e1 = e1;
    _e2 = e2;
  }

  public override Func<Zen<T>, Zen<bool>> Evaluate(State<T> state)
  {
    return t => Zen.Or(_e1.Evaluate(state)(t), _e2.Evaluate(state)(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _e1.Rename(oldVar, newVar);
    _e2.Rename(oldVar, newVar);
  }
}

using ZenLib;

namespace Gardener.AstExpr;

public class LessThan<T, TState> : Expr<bool, TState>
{
   private readonly Expr<T, TState> _e1;
   private readonly Expr<T, TState> _e2;

   public LessThan(Expr<T, TState> e1, Expr<T, TState> e2)
   {
     _e1 = e1;
     _e2 = e2;
   }
  public override Func<Zen<TState>, Zen<bool>> Evaluate(State<TState> state)
  {
    return r => Zen.Lt(_e1.Evaluate(state)(r), _e2.Evaluate(state)(r));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _e1.Rename(oldVar, newVar);
    _e2.Rename(oldVar, newVar);
  }
}

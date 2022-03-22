using ZenLib;

namespace Gardener.AstExpr;

public class LessThan<T, TState> : BinaryOpExpr<T, bool, TState>
{
   public LessThan(Expr<T, TState> e1, Expr<T, TState> e2) : base(e1, e2, Zen.Lt)
   {
   }
}

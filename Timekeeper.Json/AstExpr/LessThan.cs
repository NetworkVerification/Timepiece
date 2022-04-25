using ZenLib;

namespace Timekeeper.Json.AstExpr;

public class LessThan<T, TState> : BinaryOpExpr<T, T, bool, TState>
{
   public LessThan(Expr<T, TState> expr1, Expr<T, TState> expr2) : base(expr1, expr2, Zen.Lt)
   {
   }
}

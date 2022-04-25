using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class LessThanEqual<T, TState> : BinaryOpExpr<T, T, bool, TState>
{
  public LessThanEqual(Expr<T, TState> expr1, Expr<T, TState> expr2) : base(expr1, expr2, Zen.Leq)
  {
  }
}

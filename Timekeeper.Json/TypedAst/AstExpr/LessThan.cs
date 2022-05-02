using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class LessThan<T> : BinaryOpExpr<T, T, bool>
{
  public LessThan(Expr<T> expr1, Expr<T> expr2) : base(expr1, expr2, Zen.Lt)
  {
  }
}

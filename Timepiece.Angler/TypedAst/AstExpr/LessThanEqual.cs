using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class LessThanEqual<T> : BinaryOpExpr<T, T, bool>
{
  public LessThanEqual(Expr<T> expr1, Expr<T> expr2) : base(expr1, expr2, Zen.Leq)
  {
  }
}

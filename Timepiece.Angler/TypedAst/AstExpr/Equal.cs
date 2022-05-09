using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class Equal<T> : BinaryOpExpr<T, T, bool>
{
  public Equal(Expr<T> expr1, Expr<T> expr2) : base(expr1, expr2, Zen.Eq)
  {
  }
}

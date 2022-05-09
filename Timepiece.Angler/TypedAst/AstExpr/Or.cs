using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class Or : AssociativeBinaryExpr<bool>
{
  public Or(Expr<bool> expr1, Expr<bool> expr2) : base(expr1, expr2, Zen.Or)
  {
  }

  public Or(IEnumerable<Expr<bool>> es) : base(es, new ConstantExpr<bool>(false), Zen.Or)
  {
  }
}

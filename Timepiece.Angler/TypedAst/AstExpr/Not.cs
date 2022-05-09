using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class Not : UnaryOpExpr<bool, bool>
{
  public Not(Expr<bool> expr) : base(expr, Zen.Not)
  {
  }
}

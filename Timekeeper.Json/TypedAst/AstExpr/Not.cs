using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Not : UnaryOpExpr<bool, bool>
{
  public Not(Expr<bool> expr) : base(expr, Zen.Not)
  {
  }
}

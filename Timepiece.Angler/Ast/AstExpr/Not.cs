using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record Not : UnaryOpExpr
{
  public Not(Expr expr) : base(expr, e => Zen.Not(e))
  {
  }
}

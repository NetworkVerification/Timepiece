using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Not : UnaryOpExpr
{
  public Not(Expr expr) : base(expr, e => Zen.Not(e))
  {
  }
}

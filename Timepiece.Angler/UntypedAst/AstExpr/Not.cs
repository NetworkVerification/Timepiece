using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Not : UnaryOpExpr
{
  public Not(Expr expr) : base(expr, new Func<Zen<bool>, Zen<bool>>(Zen.Not))
  {
  }
}

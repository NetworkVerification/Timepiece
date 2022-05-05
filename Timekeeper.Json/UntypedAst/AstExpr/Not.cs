using ZenLib;

namespace Timekeeper.Json.UntypedAst.AstExpr;

public class Not : UnaryOpExpr
{
  public Not(Expr expr) : base(expr, new Func<Zen<bool>, Zen<bool>>(Zen.Not))
  {
  }
}

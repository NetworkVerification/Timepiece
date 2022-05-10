using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Plus : BinaryOpExpr
{
  public Plus(Expr expr1, Expr expr2) : base(expr1, expr2,
    (e1, e2) => Zen.Plus(e1, e2))
  {
  }
}

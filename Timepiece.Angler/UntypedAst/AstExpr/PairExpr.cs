using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class PairExpr : BinaryOpExpr
{
  public PairExpr(Expr expr1, Expr expr2) : base(expr1, expr2,
    (e1, e2) => Pair.Create(e1, e2))
  {
  }
}

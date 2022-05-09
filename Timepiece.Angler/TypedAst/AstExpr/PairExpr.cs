using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class PairExpr<TA, TB> : BinaryOpExpr<TA, TB, Pair<TA, TB>>
{
  public PairExpr(Expr<TA> first, Expr<TB> second) : base(first, second, Pair.Create)
  {
  }
}

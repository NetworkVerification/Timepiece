using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class PairExpr<TA, TB, T> : BinaryOpExpr<TA, TB, Pair<TA, TB>, T>
{
  public PairExpr(Expr<TA, T> first, Expr<TB, T> second) : base(first, second, Pair.Create)
  {
  }
}

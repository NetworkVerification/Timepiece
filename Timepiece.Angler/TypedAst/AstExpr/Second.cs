using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class Second<TA, TB> : UnaryOpExpr<Pair<TA, TB>, TB>
{
  public Second(Expr<Pair<TA, TB>> pair) : base(pair, p => p.Item2())
  {
  }
}

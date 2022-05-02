using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Second<TA, TB> : UnaryOpExpr<Pair<TA, TB>, TB>
{
  public Second(Expr<Pair<TA, TB>> pair) : base(pair, p => p.Item2())
  {
  }
}

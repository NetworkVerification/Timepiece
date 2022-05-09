using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class First<TA, TB> : UnaryOpExpr<Pair<TA, TB>, TA>
{
  public First(Expr<Pair<TA, TB>> pair) : base(pair, p => p.Item1())
  {
  }
}

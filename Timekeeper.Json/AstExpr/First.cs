using ZenLib;

namespace Timekeeper.Json.AstExpr;

public class First<TA, TB, TState> : UnaryOpExpr<Pair<TA, TB>, TA, TState>
{
  public First(Expr<Pair<TA, TB>, TState> pair) : base(pair, p => p.Item1())
  {
  }
}

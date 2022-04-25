using ZenLib;

namespace Timekeeper.Json.AstExpr;

public class Equal<T, TState> : BinaryOpExpr<T, T, bool, TState>
{
  public Equal(Expr<T, TState> expr1, Expr<T, TState> expr2) : base(expr1, expr2, Zen.Eq)
  {
  }
}

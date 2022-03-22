using ZenLib;

namespace Gardener.AstExpr;

public class LessThanEqual<T, TState> : BinaryOpExpr<T, T, bool, TState>
{
  public LessThanEqual(Expr<T, TState> e1, Expr<T, TState> e2) : base(e1, e2, Zen.Leq)
  {
  }
}

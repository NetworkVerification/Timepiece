using ZenLib;

namespace Gardener.AstExpr;

public class Plus<T, TState> : BinaryOpExpr<T, T, T, TState>
{
  public Plus(Expr<T, TState> e1, Expr<T, TState> e2) : base(e1, e2, Zen.Plus) { }
}

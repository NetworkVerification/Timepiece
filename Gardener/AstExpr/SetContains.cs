using ZenLib;

namespace Gardener.AstExpr;

public class SetContains<TState> : BinaryOpExpr<string, FBag<string>, bool, TState>
{
  public SetContains(Expr<string, TState> expr1, Expr<FBag<string>, TState> expr2) : base(expr1, expr2,
    (s, set) => set.Contains(s))
  {
  }
}

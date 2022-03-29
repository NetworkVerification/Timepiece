using ZenLib;

namespace Gardener.AstExpr;

public class SetContains<TState> : BinaryOpExpr<string, FBag<string>, bool, TState>
{
  public SetContains(Expr<string, TState> e1, Expr<FBag<string>, TState> e2) : base(e1, e2,
    (s, set) => set.Contains(s))
  {
  }
}

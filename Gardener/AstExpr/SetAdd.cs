using ZenLib;

namespace Gardener.AstExpr;

public class SetAdd<TState> : BinaryOpExpr<string, FBag<string>, FBag<string>, TState>
{
  internal SetAdd(Expr<string, TState> e1, Expr<FBag<string>, TState> e2) : base(e1, e2, (s, set) => set.Add(s))
  {
  }
}

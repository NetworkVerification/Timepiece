using ZenLib;

namespace Gardener.AstExpr;

public class SetAdd<TState> : BinaryOpExpr<string, FBag<string>, FBag<string>, TState>
{
  // TODO: change to AddIfSpace?
  public SetAdd(Expr<string, TState> expr1, Expr<FBag<string>, TState> expr2) : base(expr1, expr2, (s, set) => set.Add(s))
  {
  }
}

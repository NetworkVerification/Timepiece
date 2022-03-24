using ZenLib;

namespace Gardener.AstExpr;

public class EmptySet<TState> : Expr<FBag<string>, TState>
{
  public override Func<Zen<TState>, Zen<FBag<string>>> Evaluate(AstState<TState> astState)
  {
    return _ => Zen.Create<FBag<string>>();
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

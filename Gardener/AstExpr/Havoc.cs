using ZenLib;

namespace Gardener.AstExpr;

public class Havoc<TState> : Expr<bool, TState>
{
  public override Func<Zen<TState>, Zen<bool>> Evaluate(AstState<TState> astState)
  {
    return _ => Zen.Symbolic<bool>();
  }

  public override void Rename(string oldVar, string newVar)
  {
  }
}

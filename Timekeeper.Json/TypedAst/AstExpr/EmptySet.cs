using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class EmptySet : Expr<Set<string>>
{
  public override Func<Zen<TS>, Zen<Set<string>>> Evaluate<TS>(AstState astState)
  {
    return _ => Zen.Create<Set<string>>();
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

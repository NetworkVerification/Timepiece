using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Havoc : Expr<bool>
{
  public override Func<Zen<TS>, Zen<bool>> Evaluate<TS>(AstState astState)
  {
    return _ => Zen.Symbolic<bool>();
  }

  public override void Rename(string oldVar, string newVar)
  {
  }
}

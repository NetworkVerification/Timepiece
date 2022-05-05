using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Havoc : Expr<bool>
{
  public override Zen<bool> Evaluate(AstState astState)
  {
    return Zen.Symbolic<bool>();
  }

  public override void Rename(string oldVar, string newVar)
  {
  }
}

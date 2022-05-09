using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class EmptySet : Expr<Set<string>>
{
  public override Zen<Set<string>> Evaluate(AstState astState)
  {
    return Zen.Create<Set<string>>();
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

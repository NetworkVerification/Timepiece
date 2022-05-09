using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class None<T> : Expr<Option<T>>
{
  public override Zen<Option<T>> Evaluate(AstState astState)
  {
    return Option.Null<T>();
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class None<T> : Expr<Option<T>>
{
  public override Func<Zen<TS>, Zen<Option<T>>> Evaluate<TS>(AstState astState)
  {
    return _ => Option.Null<T>();
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

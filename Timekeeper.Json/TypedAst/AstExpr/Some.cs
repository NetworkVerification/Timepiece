using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Some<T> : UnaryOpExpr<T, Option<T>>
{
  public Some(Expr<T> value) : base(value, Option.Create)
  {
  }
}

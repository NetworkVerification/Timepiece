using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class Some<T> : UnaryOpExpr<T, Option<T>>
{
  public Some(Expr<T> value) : base(value, Option.Create)
  {
  }
}

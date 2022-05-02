using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Plus<T> : AssociativeBinaryExpr<T>
{
  public Plus(Expr<T> expr1, Expr<T> expr2) : base(expr1, expr2, Zen.Plus)
  {
  }

  public Plus(IEnumerable<Expr<T>> es, Expr<T> identity) : base(es, identity, Zen.Plus)
  {
  }
}

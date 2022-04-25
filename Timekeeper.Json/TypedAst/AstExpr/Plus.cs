using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Plus<T, TState> : AssociativeBinaryExpr<T, TState>
{
  public Plus(Expr<T, TState> expr1, Expr<T, TState> expr2) : base(expr1, expr2, Zen.Plus)
  {
  }

  public Plus(IEnumerable<Expr<T, TState>> es, Expr<T, TState> identity) : base(es, identity, Zen.Plus)
  {
  }
}

using ZenLib;

namespace Gardener.AstExpr;

public class Plus<T, TState> : AssociativeBinaryExpr<T, TState>
{
  public Plus(Expr<T, TState> e1, Expr<T, TState> e2) : base(e1, e2, Zen.Plus)
  {
  }

  public Plus(IEnumerable<Expr<T, TState>> es, Expr<T, TState> identity) : base(es, identity, Zen.Plus)
  {
  }
}

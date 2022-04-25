using ZenLib;

namespace Gardener.AstExpr;

public class Not<T> : UnaryOpExpr<bool, bool, T>
{
  public Not(Expr<bool, T> expr) : base(expr, Zen.Not)
  {
  }
}

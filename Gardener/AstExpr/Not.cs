using ZenLib;

namespace Gardener.AstExpr;

public class Not<T> : UnaryOpExpr<bool, bool, T>
{
  public Not(Expr<bool, T> e) : base(e, Zen.Not)
  {
  }
}

using Newtonsoft.Json;
using ZenLib;

namespace Gardener.AstExpr;

public class And<T> : AssociativeBinaryExpr<bool, T>
{
  public And(Expr<bool, T> e1, Expr<bool, T> e2) : base(e1, e2, Zen.And)
  {
  }

  public And(IEnumerable<Expr<bool, T>> es) : base(es, new ConstantExpr<bool, T>(true), Zen.And)
  {
  }
}

using Newtonsoft.Json;
using ZenLib;

namespace Gardener.AstExpr;

public class Or<T> : AssociativeBinaryExpr<bool, T>
{
  public Or(Expr<bool, T> e1, Expr<bool, T> e2) : base(e1, e2, Zen.Or)
  {
  }

  public Or(IEnumerable<Expr<bool, T>> es) : base(es, new ConstantExpr<bool, T>(false), Zen.Or)
  {
  }
}

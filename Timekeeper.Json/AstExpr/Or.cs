using Newtonsoft.Json;
using ZenLib;

namespace Gardener.AstExpr;

public class Or<T> : AssociativeBinaryExpr<bool, T>
{
  public Or(Expr<bool, T> expr1, Expr<bool, T> expr2) : base(expr1, expr2, Zen.Or)
  {
  }

  public Or(IEnumerable<Expr<bool, T>> es) : base(es, new ConstantExpr<bool, T>(false), Zen.Or)
  {
  }
}

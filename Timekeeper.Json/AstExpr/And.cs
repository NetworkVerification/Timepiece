using Newtonsoft.Json;
using ZenLib;

namespace Timekeeper.Json.AstExpr;

public class And<T> : AssociativeBinaryExpr<bool, T>
{
  public And(Expr<bool, T> expr1, Expr<bool, T> expr2) : base(expr1, expr2, Zen.And)
  {
  }

  [JsonConstructor]
  public And(IEnumerable<Expr<bool, T>> es) : base(es, new ConstantExpr<bool, T>(true), Zen.And)
  {
  }
}

using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class And : AssociativeBinaryExpr<bool>
{
  public And(Expr<bool> expr1, Expr<bool> expr2) : base(expr1, expr2, Zen.And)
  {
  }

  [JsonConstructor]
  public And(IEnumerable<Expr<bool>> es) : base(es, new ConstantExpr<bool>(true), Zen.And)
  {
  }
}

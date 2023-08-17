using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class SetUnion : AssociativeBinaryOpExpr
{
  private static readonly Func<dynamic, dynamic, Zen<CSet<string>>> Op = (e1, e2) => CSet.Union<string>(e1, e2);

  public SetUnion(Expr expr1, Expr expr2) : base(expr1, expr2, Op)
  {
  }

  [JsonConstructor]
  public SetUnion(IEnumerable<Expr> exprs) : base(exprs, LiteralSet.Empty(), Op)
  {
  }
}

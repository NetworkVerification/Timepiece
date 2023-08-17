using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class Or : AssociativeBinaryOpExpr
{
  private static readonly Func<dynamic, dynamic, Zen<bool>> Op = (e1, e2) => Zen.Or(e1, e2);

  public Or(Expr operand1, Expr operand2) : base(operand1, operand2, Op)
  {
  }

  [JsonConstructor]
  public Or(IEnumerable<Expr> exprs) : base(exprs, new BoolExpr(false), Op)
  {
  }
}

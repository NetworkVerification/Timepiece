using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record And : AssociativeBinaryOpExpr
{
  private static readonly Func<dynamic, dynamic, Zen<bool>> Op = (e1, e2) => Zen.And(e1, e2);

  public And(Expr operand1, Expr operand2) : base(operand1, operand2, Op)
  {
  }

  [JsonConstructor]
  public And(IEnumerable<Expr> exprs) : base(exprs, new BoolExpr(true), Op)
  {
  }
}

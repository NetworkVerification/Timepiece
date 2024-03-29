using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record NotEqual : BinaryOpExpr
{
  public NotEqual(Expr operand1, Expr operand2) : base(operand1, operand2, (e1, e2) => Zen.Not(Zen.Eq(e1, e2)))
  {
  }
}

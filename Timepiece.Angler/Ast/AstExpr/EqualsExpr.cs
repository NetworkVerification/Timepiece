namespace Timepiece.Angler.Ast.AstExpr;

public record EqualsExpr : BinaryOpExpr
{
  public EqualsExpr(Expr operand1, Expr operand2) : base(operand1, operand2,
    (num1, num2) => num1 == num2)
  {
  }
}

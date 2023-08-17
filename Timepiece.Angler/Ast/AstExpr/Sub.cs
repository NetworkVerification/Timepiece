namespace Timepiece.Angler.Ast.AstExpr;

public class Sub : BinaryOpExpr
{
  public Sub(Expr operand1, Expr operand2) : base(operand1, operand2,
    (num1, num2) => num1 - num2)
  {
  }
}

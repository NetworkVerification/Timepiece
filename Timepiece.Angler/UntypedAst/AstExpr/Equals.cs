namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Equals : BinaryOpExpr
{
  public Equals(Expr operand1, Expr operand2) : base(operand1, operand2,
    (num1, num2) => num1 == num2)
  {
  }
}

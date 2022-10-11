using Timepiece.Angler.UntypedAst.AstExpr;

namespace Timepiece.Angler.UntypedAst;

public class Equals : BinaryOpExpr
{
  public Equals(Expr operand1, Expr operand2, Func<dynamic, dynamic, dynamic> binaryOp) : base(operand1, operand2,
    (num1, num2) => num1 == num2)
  {
  }
}

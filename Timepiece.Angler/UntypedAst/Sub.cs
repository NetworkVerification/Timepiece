using Timepiece.Angler.UntypedAst.AstExpr;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public class Sub : BinaryOpExpr
{
  public Sub(Expr operand1, Expr operand2) : base(operand1, operand2,
    (Func<dynamic, dynamic, Zen<int>>) ((num1, num2) => num1 - num2))
  {
  }
}

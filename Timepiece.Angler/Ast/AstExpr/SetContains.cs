using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class SetContains : BinaryOpExpr
{
  public SetContains(Expr operand1, Expr operand2) : base(operand1, operand2,
    (s, st) => CSet.Contains<string>(st, s))
  {
  }
}

using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record SetDifference : BinaryOpExpr
{
  public SetDifference(Expr operand1, Expr operand2) : base(operand1, operand2,
    (set1, set2) => CSet.Difference(set1, set2))
  {
  }
}

using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class Subset : BinaryOpExpr
{
  public Subset(Expr operand1, Expr operand2) : base(operand1, operand2,
    (set1, set2) => CSet.IsSubsetOf(set1, set2))
  {
  }
}

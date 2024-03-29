using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record SetAdd : BinaryOpExpr
{
  public SetAdd(Expr operand1, Expr operand2) : base(operand1, operand2,
    (s, st) => CSet.Add(st, s))
  {
  }
}

using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record SetRemove : BinaryOpExpr
{
  public SetRemove(Expr operand1, Expr operand2) : base(operand1, operand2,
    (s, st) => CSet.Delete<string>(st, s))
  {
  }
}

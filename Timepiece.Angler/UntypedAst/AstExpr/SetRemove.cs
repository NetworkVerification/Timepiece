using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class SetRemove : BinaryOpExpr
{
  public SetRemove(Expr operand1, Expr operand2) : base(operand1, operand2,
    (s, st) => CSet.Delete(st, s))
  {
  }
}

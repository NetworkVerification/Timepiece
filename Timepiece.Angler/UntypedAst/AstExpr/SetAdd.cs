using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class SetAdd : BinaryOpExpr
{
  public SetAdd(Expr operand1, Expr operand2) : base(operand1, operand2,
    (s, st) => CSet.Add(st, s))
  {
  }
}

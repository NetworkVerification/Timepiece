namespace Timepiece.Angler.UntypedAst.AstExpr;

public class SetContains : BinaryOpExpr
{
  public SetContains(Expr operand1, Expr operand2) : base(operand1, operand2,
    (s, st) => st.Contains(s))
  {
  }
}

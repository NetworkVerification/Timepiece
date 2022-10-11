using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Plus : AssociativeBinaryOpExpr
{
  private static readonly Func<dynamic, dynamic, dynamic> Op = (e1, e2) => Zen.Plus(e1, e2);

  public Plus(Expr operand1, Expr operand2) : base(operand1, operand2,
    Op)
  {
  }

  public Plus(IEnumerable<Expr> exprs) : base(exprs, new ConstantExpr(0), Op)
  {
  }
}

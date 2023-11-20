using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
///   Addition of two unsigned integers.
/// </summary>
public record Plus : AssociativeBinaryOpExpr
{
  private static readonly Func<dynamic, dynamic, dynamic> Op = (e1, e2) => Zen.Plus(e1, e2);

  public Plus(Expr operand1, Expr operand2) : base(operand1, operand2,
    Op)
  {
  }

  public Plus(IEnumerable<Expr> exprs) : base(exprs, new UIntExpr(0), Op)
  {
  }
}

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class AssociativeBinaryOpExpr : BinaryOpExpr
{
  public AssociativeBinaryOpExpr(Expr operand1, Expr operand2, Func<dynamic, dynamic, dynamic> binaryOp) : base(
    operand1, operand2,
    binaryOp)
  {
  }

  public AssociativeBinaryOpExpr(IEnumerable<Expr> operands, Expr identity, Func<dynamic, dynamic, dynamic> binaryOp) :
    this(identity, FromEnumerable(operands, identity, binaryOp), binaryOp)
  {
  }

  private static AssociativeBinaryOpExpr FromEnumerable(IEnumerable<Expr> es,
    Expr identity, Func<dynamic, dynamic, dynamic> binaryOp)
  {
    var operands = es.ToArray();
    return operands.Length switch
    {
      0 => throw new ArgumentException("Invalid number of arguments to binary expression"),
      1 => new AssociativeBinaryOpExpr(operands[0], identity, binaryOp),
      _ => new AssociativeBinaryOpExpr(operands[0], FromEnumerable(operands[1..], identity, binaryOp),
        binaryOp)
    };
  }
}

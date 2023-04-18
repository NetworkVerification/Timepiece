namespace Timepiece.Angler.UntypedAst.AstExpr;

public class AssociativeBinaryOpExpr : BinaryOpExpr
{
  public AssociativeBinaryOpExpr(Expr operand1, Expr operand2, Func<dynamic, dynamic, dynamic> binaryOp) : base(
    operand1, operand2,
    binaryOp)
  {
  }

  public AssociativeBinaryOpExpr(IEnumerable<Expr> exprs, Expr identity, Func<dynamic, dynamic, dynamic> binaryOp) :
    this(FromEnumerator(exprs.GetEnumerator(), identity, binaryOp), identity, binaryOp)
  {
  }

  private static Expr FromEnumerator(IEnumerator<Expr> es, Expr acc, Func<dynamic, dynamic, dynamic> binaryOp)
  {
    while (true)
    {
      if (es is null) throw new ArgumentNullException(nameof(es), "No arguments given to binary expression");

      if (!es.MoveNext()) return acc;
      acc = new AssociativeBinaryOpExpr(acc, es.Current, binaryOp);

      /*
    var operands = es.ToArray();
    return operands.Length switch
    {
      0 => throw new ArgumentException("Binary expression received zero arguments"),
      1 => new AssociativeBinaryOpExpr(operands[0], identity, binaryOp),
      _ => new AssociativeBinaryOpExpr(operands[0], FromEnumerable(operands[1..], identity, binaryOp),
        binaryOp)
    };
*/
    }
  }
}

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class BinaryOpExpr : Expr
{
  public readonly Expr expr1;
  public readonly Expr expr2;
  public readonly dynamic binaryOp;

  public BinaryOpExpr(Expr expr1, Expr expr2, dynamic binaryOp)
  {
    this.expr1 = expr1;
    this.expr2 = expr2;
    this.binaryOp = binaryOp;
  }

  public override void Rename(string oldVar, string newVar)
  {
    expr1.Rename(oldVar, newVar);
    expr2.Rename(oldVar, newVar);
  }

  private static BinaryOpExpr FromEnumerable(IEnumerable<Expr> es,
    Expr identity, dynamic binaryOp)
  {
    var operands = es.ToArray();
    return operands.Length switch
    {
      0 => throw new ArgumentException("Invalid number of arguments to binary expression"),
      1 => new BinaryOpExpr(operands[0], identity, binaryOp),
      _ => new BinaryOpExpr(operands[0], FromEnumerable(operands[1..], identity, binaryOp),
        binaryOp)
    };
  }
}

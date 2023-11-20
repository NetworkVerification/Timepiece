namespace Timepiece.Angler.Ast.AstExpr;

public record BinaryOpExpr : Expr
{
  public readonly Func<dynamic, dynamic, dynamic> binaryOp;
  public readonly Expr expr1;
  public readonly Expr expr2;

  public BinaryOpExpr(Expr operand1, Expr operand2, Func<dynamic, dynamic, dynamic> binaryOp)
  {
    expr1 = operand1;
    expr2 = operand2;
    this.binaryOp = binaryOp;
  }

  public override void Rename(string oldVar, string newVar)
  {
    expr1.Rename(oldVar, newVar);
    expr2.Rename(oldVar, newVar);
  }
}

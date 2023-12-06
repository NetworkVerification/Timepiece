namespace Timepiece.Angler.Ast.AstExpr;

public record BinaryOpExpr(Expr expr1, Expr expr2, Func<dynamic, dynamic, dynamic> binaryOp) : Expr
{
  public readonly Func<dynamic, dynamic, dynamic> binaryOp = binaryOp;
  public readonly Expr expr1 = expr1;
  public readonly Expr expr2 = expr2;

  public override void Rename(string oldVar, string newVar)
  {
    expr1.Rename(oldVar, newVar);
    expr2.Rename(oldVar, newVar);
  }
}

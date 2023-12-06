namespace Timepiece.Angler.Ast.AstExpr;

public record UnaryOpExpr(Expr expr, Func<dynamic, dynamic> unaryOp) : Expr
{
  public readonly Expr expr = expr;
  public readonly Func<dynamic, dynamic> unaryOp = unaryOp;

  public override void Rename(string oldVar, string newVar)
  {
    expr.Rename(oldVar, newVar);
  }
}

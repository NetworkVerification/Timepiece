namespace Timekeeper.Json.UntypedAst.AstExpr;

public class UnaryOpExpr : Expr
{
  public readonly Expr expr;
  public readonly dynamic unaryOp;

  public UnaryOpExpr(Expr expr, dynamic unaryOp)
  {
    this.expr = expr;
    this.unaryOp = unaryOp;
  }

  public override void Rename(string oldVar, string newVar)
  {
    expr.Rename(oldVar, newVar);
  }
}

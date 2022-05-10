namespace Timepiece.Angler.UntypedAst.AstExpr;

public class UnaryOpExpr : Expr
{
  public readonly Expr expr;
  public readonly Func<dynamic,dynamic> unaryOp;

  public UnaryOpExpr(Expr expr, Func<dynamic,dynamic> unaryOp)
  {
    this.expr = expr;
    this.unaryOp = unaryOp;
  }

  public override void Rename(string oldVar, string newVar)
  {
    expr.Rename(oldVar, newVar);
  }
}

namespace Timepiece.Angler.Ast.AstExpr;

public class VariadicExpr : Expr
{
  public VariadicExpr(params Expr[] exprs)
  {
    Exprs = exprs;
  }

  /// <summary>
  ///   The sub-expressions of the expression.
  /// </summary>
  public Expr[] Exprs { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    foreach (var expr in Exprs) expr.Rename(oldVar, newVar);
  }
}

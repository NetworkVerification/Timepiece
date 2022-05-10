using Timepiece.Angler.UntypedAst.AstExpr;

namespace Timepiece.Angler.UntypedAst.AstStmt;

public class Return : Statement
{
  public Return(Expr expr)
  {
    Expr = expr;
  }

  public Expr Expr { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Expr.Rename(oldVar, newVar);
  }

  public override Statement Bind(string variable)
  {
    return new Assign(variable, Expr);
  }
}

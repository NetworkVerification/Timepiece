using Timepiece.Angler.UntypedAst.AstExpr;

namespace Timepiece.Angler.UntypedAst.AstStmt;

public class Assign : Statement
{
  public Assign(string var, Expr expr)
  {
    Var = var;
    Expr = expr;
  }

  public string Var { get; set; }
  public Expr Expr { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Expr.Rename(oldVar, newVar);
    if (Var == oldVar) Var = newVar;
  }
}

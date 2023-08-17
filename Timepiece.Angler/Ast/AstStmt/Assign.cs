using Timepiece.Angler.Ast.AstExpr;

namespace Timepiece.Angler.Ast.AstStmt;

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

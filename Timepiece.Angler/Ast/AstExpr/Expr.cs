namespace Timepiece.Angler.Ast.AstExpr;

public abstract record Expr : IRenameable
{
  public abstract void Rename(string oldVar, string newVar);
}

namespace Timepiece.Angler.Ast.AstExpr;

public abstract class Expr : IRenameable
{
  public abstract void Rename(string oldVar, string newVar);
}

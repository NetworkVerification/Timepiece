namespace Timepiece.Angler.Ast.AstStmt;

public abstract record Statement : IRenameable
{
  public abstract void Rename(string oldVar, string newVar);
}

namespace Timepiece.Angler.UntypedAst.AstStmt;

public abstract class Statement
{
  public abstract void Rename(string oldVar, string newVar);
}

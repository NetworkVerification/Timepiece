namespace Timepiece.Angler.UntypedAst.AstStmt;

public abstract class Statement : IRenameable
{
  public abstract void Rename(string oldVar, string newVar);

  public abstract Statement Bind(string variable);
}

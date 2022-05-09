namespace Timepiece.Angler.UntypedAst;

public abstract class Statement
{
  public abstract void Rename(string oldVar, string newVar);
}

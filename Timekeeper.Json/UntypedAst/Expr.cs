namespace Timekeeper.Json.UntypedAst;

public abstract class Expr
{
  public abstract void Rename(string oldVar, string newVar);
}

namespace Timekeeper.Json.UntypedAst.AstExpr;

public abstract class Expr
{
  public abstract void Rename(string oldVar, string newVar);
}

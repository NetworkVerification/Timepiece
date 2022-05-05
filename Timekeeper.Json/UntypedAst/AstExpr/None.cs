namespace Timekeeper.Json.UntypedAst.AstExpr;

public class None : Expr
{
  public Type innerType;

  public None(Type innerType)
  {
    this.innerType = innerType;
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

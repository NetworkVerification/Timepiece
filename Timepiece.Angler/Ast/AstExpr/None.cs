namespace Timepiece.Angler.Ast.AstExpr;

public record None : Expr
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

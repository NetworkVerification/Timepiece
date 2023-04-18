namespace Timepiece.Angler.UntypedAst.AstExpr;

public class ConstantExpr : Expr
{
  public readonly Func<dynamic, dynamic> constructor;
  public readonly dynamic value;

  public ConstantExpr(dynamic value, Func<dynamic, dynamic> constructor)
  {
    this.value = value;
    this.constructor = constructor;
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

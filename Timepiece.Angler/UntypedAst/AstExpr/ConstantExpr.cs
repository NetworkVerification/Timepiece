namespace Timepiece.Angler.UntypedAst.AstExpr;

public class ConstantExpr : Expr
{
  public readonly dynamic value;
  public readonly Func<dynamic, dynamic> constructor;

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

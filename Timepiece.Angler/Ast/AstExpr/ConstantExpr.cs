namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
/// An expression that represents a primitive value.
/// Bundles a constructor to produce an appropriate manipulable type:
/// typically this would be some form of Zen expression.
/// </summary>
public record ConstantExpr : Expr
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

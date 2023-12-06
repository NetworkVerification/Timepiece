namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
/// An expression that represents a primitive value.
/// Bundles a constructor to produce an appropriate manipulable type:
/// typically this would be some form of Zen expression.
/// </summary>
public record ConstantExpr(dynamic value, Func<dynamic, dynamic> constructor) : Expr
{
  public readonly Func<dynamic, dynamic> constructor = constructor;
  public readonly dynamic value = value;

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

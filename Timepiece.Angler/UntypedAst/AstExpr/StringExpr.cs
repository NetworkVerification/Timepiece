namespace Timepiece.Angler.UntypedAst.AstExpr;

public class StringExpr : ConstantExpr
{
  // strings used with CSets should be literal C# values, not Zen<string>;
  // hence the constructor returns the value unchanged
  public StringExpr(string value) : base(value, v => v)
  {
  }
}

namespace Timepiece.Angler.Ast.AstExpr;

public class RegexExpr : ConstantExpr
{
  public RegexExpr(string regex) : base(regex, r => r)
  {
  }
}

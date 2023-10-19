namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
///   A regular expression.
///   For now, this is just handled like a string (see <see cref="StringExpr" />).
/// </summary>
public class RegexExpr : ConstantExpr
{
  public RegexExpr(string regex) : base(regex, r => r)
  {
  }
}

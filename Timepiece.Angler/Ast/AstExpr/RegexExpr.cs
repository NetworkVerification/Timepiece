namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
///   A regular expression.
///   For now, this is just handled like a string (see <see cref="StringExpr" />).
/// </summary>
public record RegexExpr : ConstantExpr
{
  public RegexExpr(string regex) : base(regex, r => r)
  {
  }
}

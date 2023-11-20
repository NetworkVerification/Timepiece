namespace Timepiece.Angler.Ast.AstExpr;

public record PrefixMatches(dynamic IpWildcard, dynamic PrefixLengthRange) : Expr
{
  public dynamic IpWildcard { get; set; } = IpWildcard;

  public dynamic PrefixLengthRange { get; set; } = PrefixLengthRange;

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

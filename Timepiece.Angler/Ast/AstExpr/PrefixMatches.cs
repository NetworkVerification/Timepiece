namespace Timepiece.Angler.Ast.AstExpr;

public class PrefixMatches : Expr
{
  public PrefixMatches(dynamic ipWildcard, dynamic prefixLengthRange)
  {
    IpWildcard = ipWildcard;
    PrefixLengthRange = prefixLengthRange;
  }

  public dynamic IpWildcard { get; set; }

  public dynamic PrefixLengthRange { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

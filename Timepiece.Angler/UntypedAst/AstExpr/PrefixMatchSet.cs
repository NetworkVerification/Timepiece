namespace Timepiece.Angler.UntypedAst.AstExpr;

public class PrefixMatchSet : Expr
{
  public dynamic Deny { get; set; }
  public dynamic Permit { get; set; }

  public PrefixMatchSet(dynamic deny, dynamic permit)
  {
    Deny = deny;
    Permit = permit;
  }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

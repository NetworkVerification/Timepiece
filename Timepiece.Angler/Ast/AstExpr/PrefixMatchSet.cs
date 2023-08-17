namespace Timepiece.Angler.Ast.AstExpr;

public class PrefixMatchSet : Expr
{
  public PrefixMatchSet(dynamic deny, dynamic permit)
  {
    Deny = deny;
    Permit = permit;
  }

  public dynamic Deny { get; set; }
  public dynamic Permit { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

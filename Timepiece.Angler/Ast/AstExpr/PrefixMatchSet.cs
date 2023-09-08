namespace Timepiece.Angler.Ast.AstExpr;

public class PrefixMatchSet : Expr
{
  public PrefixMatchSet(dynamic prefix, dynamic list)
  {
    Prefix = prefix;
    FilterList = list;
  }

  public Expr Prefix { get; set; }
  public RouteFilterList FilterList { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

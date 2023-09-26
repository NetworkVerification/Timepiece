namespace Timepiece.Angler.Ast.AstExpr;

public class RouteFilterListExpr : Expr
{
  public RouteFilterListExpr(RouteFilterList list)
  {
    FilterList = list;
  }

  public RouteFilterList FilterList { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

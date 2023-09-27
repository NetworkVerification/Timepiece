namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
/// An expression wrapping a <c>RouteFilterList</c>.
/// </summary>
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

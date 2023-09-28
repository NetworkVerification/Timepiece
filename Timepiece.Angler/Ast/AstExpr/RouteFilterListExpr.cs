using Timepiece.DataTypes;
using ZenLib;

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

  public Zen<bool> Contains(Zen<Ipv4Prefix> prefix) => FilterList.Permits(prefix);
}

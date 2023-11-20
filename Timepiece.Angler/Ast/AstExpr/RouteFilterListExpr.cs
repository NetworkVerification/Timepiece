using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
///   An expression wrapping a <c>RouteFilterList</c>.
/// </summary>
/// <remarks>For simplicity, we treat a route filter list as a kind of constant,
/// although it does not look like a "traditional" constant and notably
/// does not lift the <c>RouteFilterList</c> to a <c>Zen{RouteFilterList}</c>.</remarks>
public record RouteFilterListExpr : ConstantExpr
{
  public RouteFilterListExpr(RouteFilterList list) : base(list, x => x)
  {
    FilterList = list;
  }

  public RouteFilterList FilterList { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    ;
  }

  public Zen<bool> Contains(Zen<Ipv4Prefix> prefix)
  {
    return FilterList.Matches(prefix);
  }
}

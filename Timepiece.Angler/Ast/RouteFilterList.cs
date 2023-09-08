using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
/// A route filter list for testing if a route should be filtered according to its prefix.
/// Composed of a series of rules (RouteFilterLines) that are evaluated in-order against the prefix
/// until a match is found.
/// If no rule matches, we default to denying (filtering) the route.
/// </summary>
public class RouteFilterList
{
  public RouteFilterList()
  {
    Lines = System.Array.Empty<RouteFilterLine>();
  }

  public RouteFilterList(RouteFilterLine[] lines)
  {
    Lines = lines;
  }

  private RouteFilterLine[] Lines { get; set; }

  /// <summary>
  /// Return a Zen expression encoding if the route filter list matches the given prefix.
  /// </summary>
  /// <param name="prefix"></param>
  /// <returns></returns>
  public Zen<bool> Permits(Zen<Ipv4Prefix> prefix)
  {
    return Lines
      .Reverse() // we build up the successive cases by going in reverse order
      .Aggregate(
        Zen.False(), // the implicit default if no line matches is Zen.False() (deny)
        (cases, line) =>
        {
          // if the line matches the given prefix's address,
          var wildcardMatch = Zen.Constant(line.Wildcard).ContainsIp(prefix.GetPrefix());
          // and the prefix length is within the allowed range
          var lengthMatch = Zen.And(line.MinLength <= prefix.GetPrefixLength(),
            prefix.GetPrefixLength() <= line.MaxLength);
          // then we perform the desired action
          return Zen.If(Zen.And(wildcardMatch, lengthMatch), line.Action, cases);
        });
  }
}

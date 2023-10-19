using Newtonsoft.Json;
using Timepiece.DataTypes;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Angler.Ast;

/// <summary>
///   A route filter list for testing if a route should be filtered according to its prefix.
///   Composed of a series of rules (RouteFilterLines) that are evaluated in-order against the prefix
///   until a match is found.
///   If no rule matches, we default to denying (filtering) the route.
/// </summary>
public class RouteFilterList
{
  public RouteFilterList()
  {
    Lines = Array.Empty<RouteFilterLine>();
  }

  [JsonConstructor]
  public RouteFilterList(RouteFilterLine[] lines)
  {
    Lines = lines;
  }

  private RouteFilterLine[] Lines { get; }

  /// <summary>
  ///   Return a Zen expression encoding if the route filter list matches the given prefix.
  /// </summary>
  /// <param name="prefix"></param>
  /// <returns></returns>
  public Zen<bool> Permits(Zen<Ipv4Prefix> prefix)
  {
    return Lines
      .Reverse() // we build up the successive cases by going in reverse order
      .Aggregate(
        Zen.False(), // the implicit default if no line matches is Zen.False() (deny)
        (cases, line) => Zen.If(Zen.Constant(line.Wildcard).MatchesPrefix(prefix, line.MinLength, line.MaxLength),
          line.Action, cases));
  }
}

using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
/// A line of a route filter list.
/// </summary>
public class RouteFilterLine
{
  public RouteFilterLine(bool action, Ipv4Wildcard wildcard, UInt<_6> minLength, UInt<_6> maxLength)
  {
    Action = action;
    Wildcard = wildcard;
    MinLength = minLength;
    MaxLength = maxLength;
  }

  /// <summary>
  /// The action to take. If true, permits the route; if false, denies it.
  /// </summary>
  public bool Action { get; set; }

  /// <summary>
  /// The IP wildcard to test for a match.
  /// </summary>
  public Ipv4Wildcard Wildcard { get; set; }

  /// <summary>
  /// The minimum matching length of the tested prefix.
  /// </summary>
  public UInt<_6> MinLength { get; set; }

  /// <summary>
  /// The maximum matching length of the tested prefix.
  /// </summary>
  public UInt<_6> MaxLength { get; set; }
}

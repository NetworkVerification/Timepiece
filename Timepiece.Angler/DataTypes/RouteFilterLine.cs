using Newtonsoft.Json;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.DataTypes;

/// <summary>
///   A line of a route filter list.
/// </summary>
public record struct RouteFilterLine(bool Action, Ipv4Wildcard Wildcard, UInt<_6> MinLength, UInt<_6> MaxLength)
{
  [JsonConstructor]
  public RouteFilterLine(bool action, Ipv4Wildcard wildcard, uint minLength, uint maxLength) : this(action, wildcard,
    new UInt<_6>(minLength), new UInt<_6>(maxLength))
  {
  }

  /// <summary>
  ///   The action to take. If true, permits the route; if false, denies it.
  /// </summary>
  public bool Action { get; set; } = Action;

  /// <summary>
  ///   The IP wildcard to test for a match.
  /// </summary>
  public Ipv4Wildcard Wildcard { get; set; } = Wildcard;

  /// <summary>
  ///   The minimum matching length of the tested prefix.
  /// </summary>
  public UInt<_6> MinLength { get; set; } = MinLength;

  /// <summary>
  ///   The maximum matching length of the tested prefix.
  /// </summary>
  public UInt<_6> MaxLength { get; set; } = MaxLength;
}

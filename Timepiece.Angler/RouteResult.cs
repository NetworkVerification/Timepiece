using ZenLib;

namespace Timepiece.Angler;

/// <summary>
///   A representation of the result of evaluating a routing policy.
///   See Batfish's
///   <a
///     href="https://github.com/batfish/batfish/blob/master/projects/batfish-common-protocol/src/main/java/org/batfish/datamodel/routing_policy/Result.java">
///     Result
///   </a>
///   and Minesweeper's
///   <a
///     href="https://github.com/batfish/batfish/blob/master/projects/minesweeper/src/main/java/org/batfish/minesweeper/bdd/TransferResult.java">
///     TransferResult
///   </a>
///   classes for reference.
/// </summary>
[ZenObject]
public class RouteResult : IEquatable<RouteResult>
{
  public RouteResult(bool exit, bool fallthrough, bool returned, bool value)
  {
    Exit = exit;
    Fallthrough = fallthrough;
    Returned = returned;
    Value = value;
  }

  public RouteResult()
  {
    Exit = false;
    Fallthrough = false;
    Returned = false;
    Value = false;
  }

  /// <summary>
  ///   Whether the result has exited.
  /// </summary>
  public bool Exit { get; set; }

  /// <summary>
  ///   Whether the result has fallen through.
  /// </summary>
  public bool Fallthrough { get; set; }

  /// <summary>
  ///   Whether the result has returned.
  /// </summary>
  public bool Returned { get; set; }

  /// <summary>
  ///   The value associated with the result.
  ///   True for accept, false for reject.
  /// </summary>
  public bool Value { get; set; }

  public bool Equals(RouteResult? other)
  {
    if (ReferenceEquals(null, other)) return false;
    if (ReferenceEquals(this, other)) return true;
    return Exit == other.Exit && Fallthrough == other.Fallthrough && Returned == other.Returned && Value == other.Value;
  }

  public override bool Equals(object? obj)
  {
    if (ReferenceEquals(null, obj)) return false;
    if (ReferenceEquals(this, obj)) return true;
    return obj.GetType() == GetType() && Equals((RouteResult) obj);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Exit, Fallthrough, Returned, Value);
  }

  public static bool operator ==(RouteResult? left, RouteResult? right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(RouteResult? left, RouteResult? right)
  {
    return !Equals(left, right);
  }

  public override string ToString()
  {
    return $"RouteResult(Exit={Exit},Fallthrough={Fallthrough},Returned={Returned},Value={Value})";
  }
}

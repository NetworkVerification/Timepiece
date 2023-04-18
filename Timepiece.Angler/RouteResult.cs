using ZenLib;

namespace Timepiece.Angler;

[ZenObject]
public class RouteResult
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

  public override string ToString()
  {
    return $"RouteResult(Exit={Exit},Fallthrough={Fallthrough},Returned={Returned},Value={Value})";
  }
}

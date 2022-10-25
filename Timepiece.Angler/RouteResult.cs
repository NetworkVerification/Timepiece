using ZenLib;

namespace Timepiece.Angler;

[ZenObject]
public class RouteResult
{
  public bool Returned { get; set; }
  public bool Exit { get; set; }
  public bool Value { get; set; }
  public bool Fallthrough { get; set; }

  public RouteResult(bool returned, bool exit, bool value, bool fallthrough)
  {
    Returned = returned;
    Exit = exit;
    Value = value;
    Fallthrough = fallthrough;
  }

  public RouteResult()
  {
    Returned = false;
    Exit = false;
    Value = false;
    Fallthrough = false;
  }
}

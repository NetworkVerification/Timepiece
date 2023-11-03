using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
///   Representation of a return value and a route being manipulated
///   in the current environment.
/// </summary>
/// <typeparam name="RouteType">The type of routes.</typeparam>
public record ReturnRoute<RouteType>(Zen<RouteType> Route, dynamic ReturnValue)
{
  /// <summary>
  ///   Construct a ReturnRoute where the route is the returned value.
  /// </summary>
  /// <param name="route"></param>
  public ReturnRoute(Zen<RouteType> route) : this(route, route)
  {
  }

  /// <summary>
  ///   Return true if both environments' routes are logically equal.
  ///   May not produce expected results if Zen's equality test cannot handle the RouteType.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public bool EqualRoutes(ReturnRoute<RouteType> other)
  {
    return !Zen.Not(Zen.Eq(Route, other.Route)).Solve().IsSatisfiable();
  }
}

using ZenLib;

namespace Timepiece.Angler;

/// <summary>
///   Representation of a return value and a route being manipulated
///   in the current environment.
/// </summary>
/// <typeparam name="RouteType">The type of routes.</typeparam>
public record ReturnEnvironment<RouteType>(Zen<RouteType> Route, dynamic ReturnValue)
{
  /// <summary>
  ///   Construct a ReturnEnvironment where the route is the returned value.
  /// </summary>
  /// <param name="route"></param>
  public ReturnEnvironment(Zen<RouteType> route) : this(route, route)
  {
  }

  /// <summary>
  ///   Return true if both environments' routes are logically equal.
  ///   May not produce expected results if Zen's equality test cannot handle the RouteType.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public bool EqualRoutes(ReturnEnvironment<RouteType> other)
  {
    return !Zen.Not(Zen.Eq(Route, other.Route)).Solve().IsSatisfiable();
  }
}

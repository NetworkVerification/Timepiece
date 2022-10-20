using ZenLib;

namespace Timepiece.Angler;

public class Environment<T>
{
  public Environment(Zen<T> route, dynamic returnValue)
  {
    this.route = route;
    this.returnValue = returnValue;
  }

  public Environment(Zen<T> route) : this(route, route)
  {
  }

  public readonly Zen<T> route;
  public readonly dynamic returnValue;

  public Environment<T> WithRoute(Zen<T> r)
  {
    return new Environment<T>(r, returnValue);
  }

  public Environment<T> WithValue(dynamic v)
  {
    return new Environment<T>(route, v);
  }

  /// <summary>
  /// Checks that two environments are equal.
  /// This is reference equality for the routes.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  protected bool Equals(Environment<T> other)
  {
    return route.Equals(other.route) && returnValue.Equals(other.returnValue);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(route, returnValue);
  }

  /// <summary>
  /// Return true if both environments' routes are logically equal.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public bool EqualRoutes(Environment<T> other)
  {
    return !Zen.Not(Zen.Eq(route, other.route)).Solve().IsSatisfiable();
  }
}

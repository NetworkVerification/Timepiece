using ZenLib;

namespace Timekeeper.Json;

/// <summary>
/// Representation of a Zen<T> -> Zen<T> function in progress
/// that we construct from the AST.
/// </summary>
public class AstRouteFunction<T>
{
  public string argument;
  public Func<Zen<T>, Zen<T>> func;

  public AstRouteFunction(string argument, Func<Zen<T>, Zen<T>> func)
  {
    this.argument = argument;
    this.func = func;
  }

  /// <summary>
  /// Join two AstRouteFunctions using the given guard as an if expression.
  /// If the guard evaluates to true, use the current route function;
  /// if it evaluates to false, use the other route function.
  /// </summary>
  /// <param name="other">A second AstRouteFunction.</param>
  /// <param name="guard">A function representing a guard.</param>
  /// <returns>A new AstRouteFunction composing the arguments.</returns>
  /// <exception cref="ArgumentException">If the functions have different arguments.</exception>
  public AstRouteFunction<T> Join(AstRouteFunction<T> other, Func<Zen<T>, Zen<bool>> guard)
  {
    if (argument != other.argument)
    {
      throw new ArgumentException("Argument must have the same name for both AstRouteFunctions.", nameof(other));
    }

    return new AstRouteFunction<T>(argument, t => Zen.If(guard(t), func(t), other.func(t)));
  }
}

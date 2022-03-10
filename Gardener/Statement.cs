using ZenLib;

namespace Gardener;

public abstract class Statement<T, TState>
{
  public abstract State<TState> Evaluate(State<TState> state);

  /// <summary>
  /// Convert the statement to an assignment to a given variable.
  /// Return statements are converted to assignments, while other
  /// types of statements are recursed (if, seq) or ignore the bind (assign).
  /// </summary>
  /// <param name="var">The name of the variable to bind.</param>
  /// <returns>A new statement assigning the result </returns>
  public abstract Statement<Unit, TState> Bind(string var);
}

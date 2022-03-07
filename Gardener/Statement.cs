using ZenLib;

namespace Gardener;

public abstract class Statement<T>
{
  public abstract State Evaluate(State state);

  /// <summary>
  /// Convert the statement to an assignment to a given variable.
  /// Return statements are converted to assignments, while other
  /// types of statements are recursed (if, seq) or ignore the bind (assign).
  /// </summary>
  /// <param name="var">The name of the variable to bind.</param>
  /// <returns>A new statement assigning the result </returns>
  public abstract Statement<Unit> Bind(string var);
}

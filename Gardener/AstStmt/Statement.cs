using ZenLib;

namespace Gardener.AstStmt;

public abstract class Statement<T, TState> : IRenameable
{
  public abstract AstState<TState> Evaluate(AstState<TState> astState);

  /// <summary>
  /// Convert the statement to an assignment to a given variable.
  /// Return statements are converted to assignments, while other
  /// types of statements are recursed (if, seq) or ignore the bind (assign).
  /// </summary>
  /// <param name="var">The name of the variable to bind.</param>
  /// <returns>A new statement assigning the result </returns>
  public abstract Statement<Unit, TState> Bind(string var);

  /// <summary>
  /// Rename all instances of assignments to a variable oldVar in the statement
  /// to a new variable newVar.
  /// </summary>
  /// <param name="oldVar">The variable name to rename.</param>
  /// <param name="newVar">The replacement variable name.</param>
  /// <returns>The same statement but with a new variable name.</returns>
  public abstract void Rename(string oldVar, string newVar);
}

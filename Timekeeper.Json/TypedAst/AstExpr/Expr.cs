using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public abstract class Expr<T, TState> : IRenameable, IEvaluable<TState, T>
{
  public abstract Func<Zen<TState>, Zen<T>> Evaluate(AstState<TState> astState);

  /// <summary>
  ///   Rename (in-place) all instances of assignments to a variable oldVar in the expression
  ///   to a new variable newVar.
  /// </summary>
  /// <param name="oldVar">The variable name to rename.</param>
  /// <param name="newVar">The replacement variable name.</param>
  public abstract void Rename(string oldVar, string newVar);
}

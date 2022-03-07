using ZenLib;

namespace Gardener;

public class Assign<T> : Statement<Unit>
{
  public Assign(string var, Expr<T> expr)
  {
    Var = var;
    Expr = expr;
  }

  /// <summary>
  /// Right-hand side of the assignment.
  /// </summary>
  public Expr<T> Expr { get; set; }

  /// <summary>
  /// The variable to assign.
  /// </summary>
  public string Var { get; set; }

  public override State Evaluate(State state)
  {
    state.Add(Var, Expr.Evaluate<T>(state));
    return state;
  }

  public override Statement<Unit> Bind(string var)
  {
    return this;
  }
}

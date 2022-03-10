using ZenLib;

namespace Gardener;

public class Assign<T> : Statement<Unit, T>
{
  public Assign(string var, Expr<T, T> expr)
  {
    Var = var;
    Expr = expr;
  }

  /// <summary>
  /// Right-hand side of the assignment.
  /// </summary>
  public Expr<T, T> Expr { get; set; }

  /// <summary>
  /// The variable to assign.
  /// </summary>
  public string Var { get; set; }

  public override State<T> Evaluate(State<T> state)
  {
    state.Add(Var, Expr.Evaluate(state));
    return state;
  }

  public override Statement<Unit, T> Bind(string var)
  {
    return this;
  }
}

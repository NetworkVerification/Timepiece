using ZenLib;

namespace Gardener;

public class Assign : Statement
{
  public Assign(string var, Expr expr)
  {
    Var = var;
    Expr = expr;
  }

  /// <summary>
  /// Right-hand side of the assignment.
  /// </summary>
  public Expr Expr { get; set; }

  /// <summary>
  /// The variable to assign.
  /// </summary>
  public string Var { get; set; }

  public override State Evaluate(State state)
  {
    state.Add(Var, Expr.Evaluate(state));
    return state;
  }

  public override Func<Zen<dynamic>, Zen<dynamic>> ToZen()
  {
    throw new NotImplementedException();
  }
}

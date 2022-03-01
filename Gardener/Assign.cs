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

  public override Dictionary<string, dynamic> Evaluate(Dictionary<string, dynamic> state)
  {
    state.Add(Var, Expr.Evaluate());
    return state;
  }

  public override Func<Zen<dynamic>, Zen<dynamic>> ToZen()
  {
    throw new NotImplementedException();
  }
}

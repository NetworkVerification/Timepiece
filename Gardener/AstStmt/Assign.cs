using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstStmt;

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
    state[Var] = Expr.Evaluate(state);
    return state;
  }

  public override Statement<Unit, T> Bind(string var)
  {
    return this;
  }

  public override void Rename(string oldVar, string newVar)
  {
    if (Var.Equals(oldVar))
    {
      Var = newVar;
    }
    Expr.Rename(oldVar, newVar);
  }
}

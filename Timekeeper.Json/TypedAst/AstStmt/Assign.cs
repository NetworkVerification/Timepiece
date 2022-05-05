using Timekeeper.Json.TypedAst.AstExpr;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstStmt;

public class Assign<T> : Statement<Unit>
{
  public Assign(string var, Expr<T> expr)
  {
    Var = var;
    Expr = expr;
  }

  /// <summary>
  ///   Right-hand side of the assignment.
  /// </summary>
  public Expr<T> Expr { get; set; }

  /// <summary>
  ///   The variable to assign.
  /// </summary>
  public string Var { get; set; }

  public override AstState Evaluate<TS>(AstState astState)
  {
    astState[Var] = Expr.Evaluate(astState);
    return astState;
  }

  public override Statement<Unit> Bind(string var)
  {
    return this;
  }

  public override void Rename(string oldVar, string newVar)
  {
    if (Var.Equals(oldVar)) Var = newVar;
    Expr.Rename(oldVar, newVar);
  }
}

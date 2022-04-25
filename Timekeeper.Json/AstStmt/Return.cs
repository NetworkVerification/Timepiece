using Timekeeper.Json.AstExpr;
using ZenLib;

namespace Timekeeper.Json.AstStmt;

public class Return<T>: Statement<T, T>
{
  public Return(Expr<T, T> expr)
  {
    Expr = expr;
  }

  public Expr<T, T> Expr { get; set; }

  public override AstState<T> Evaluate(AstState<T> astState)
  {
    astState.Return = Expr.Evaluate(astState);
    return astState;
  }

  public override Statement<Unit, T> Bind(string var)
  {
    return new Assign<T>(var, Expr);
  }

  public override void Rename(string oldVar, string newVar)
  {
    Expr.Rename(oldVar, newVar);
  }
}

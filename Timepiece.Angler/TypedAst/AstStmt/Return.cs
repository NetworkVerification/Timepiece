using Timepiece.Angler.TypedAst.AstExpr;
using ZenLib;

namespace Timepiece.Angler.TypedAst.AstStmt;

public class Return<T> : Statement<T>
{
  public Return(Expr<T> expr)
  {
    Expr = expr;
  }

  public Expr<T> Expr { get; set; }

  public override AstState Evaluate<TS>(AstState astState)
  {
    astState.Return = Expr.Evaluate(astState);
    return astState;
  }

  public override Statement<Unit> Bind(string var)
  {
    return new Assign<T>(var, Expr);
  }

  public override void Rename(string oldVar, string newVar)
  {
    Expr.Rename(oldVar, newVar);
  }
}

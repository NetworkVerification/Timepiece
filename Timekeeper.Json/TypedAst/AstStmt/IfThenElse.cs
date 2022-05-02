using Timekeeper.Json.TypedAst.AstExpr;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstStmt;

public class IfThenElse<T> : Statement<T>
{
  public IfThenElse(Expr<bool> guard, Statement<T> trueStatement, Statement<T> falseStatement)
  {
    Guard = guard;
    TrueStatement = trueStatement;
    FalseStatement = falseStatement;
  }

  public Expr<bool> Guard { get; }
  public Statement<T> TrueStatement { get; }
  public Statement<T> FalseStatement { get; }

  public override AstState Evaluate<TS>(AstState astState)
  {
    var trueState = TrueStatement.Evaluate<TS>(astState);
    var falseState = FalseStatement.Evaluate<TS>(astState);
    trueState.Join(falseState, Guard.Evaluate<TS>(astState));
    return trueState;
  }

  public override Statement<Unit> Bind(string var)
  {
    return new IfThenElse<Unit>(Guard, TrueStatement.Bind(var), FalseStatement.Bind(var));
  }

  public override void Rename(string oldVar, string newVar)
  {
    Guard.Rename(oldVar, newVar);
    TrueStatement.Rename(oldVar, newVar);
    FalseStatement.Rename(oldVar, newVar);
  }
}

using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstStmt;

public class IfThenElse<T, TState> : Statement<T, TState>
{
  public Expr<bool, TState> Guard { get; }
  public Statement<T, TState> TrueStatement { get; }
  public Statement<T, TState> FalseStatement { get; }
  public IfThenElse(Expr<bool, TState> guard, Statement<T, TState> trueStatement, Statement<T, TState> falseStatement)
  {
    Guard = guard;
    TrueStatement = trueStatement;
    FalseStatement = falseStatement;
  }

  public override State<TState> Evaluate(State<TState> state)
  {
    var trueState = TrueStatement.Evaluate(state);
    var falseState = FalseStatement.Evaluate(state);
    trueState.Join(falseState, Guard.Evaluate(state));
    return trueState;
  }

  public override Statement<Unit, TState> Bind(string var)
  {
    return new IfThenElse<Unit, TState>(Guard, TrueStatement.Bind(var), FalseStatement.Bind(var));
  }

  public override void Rename(string oldVar, string newVar)
  {
    Guard.Rename(oldVar, newVar);
    TrueStatement.Rename(oldVar, newVar);
    FalseStatement.Rename(oldVar, newVar);
  }
}

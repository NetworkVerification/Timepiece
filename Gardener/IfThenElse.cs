using ZenLib;

namespace Gardener;

public class IfThenElse<T> : Statement<T>
{
  public Expr<bool> Guard { get; }
  public Statement<T> TrueStatement { get; }
  public Statement<T> FalseStatement { get; }
  public IfThenElse(Expr<bool> guard, Statement<T> trueStatement, Statement<T> falseStatement)
  {
    Guard = guard;
    TrueStatement = trueStatement;
    FalseStatement = falseStatement;
  }

  public override State Evaluate(State state)
  {
    var trueState = TrueStatement.Evaluate(state);
    var falseState = FalseStatement.Evaluate(state);
    trueState.Join(falseState, Guard.Evaluate<object>(state));
    return trueState;
  }

  public override Statement<Unit> Bind(string var)
  {
    return new IfThenElse<Unit>(Guard, TrueStatement.Bind(var), FalseStatement.Bind(var));
  }
}

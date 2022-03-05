using ZenLib;

namespace Gardener;

public class IfThenElse : Statement
{
  public Expr<bool> Guard { get; }
  public Statement TrueStatement { get; }
  public Statement FalseStatement { get; }
  public IfThenElse(Expr<bool> guard, Statement trueStatement, Statement falseStatement)
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
}

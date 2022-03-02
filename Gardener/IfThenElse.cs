using ZenLib;

namespace Gardener;

public class IfThenElse : Statement
{
  public Expr Guard { get; }
  public Statement TrueStatement { get; }
  public Statement FalseStatement { get; }
  public IfThenElse(Expr guard, Statement trueStatement, Statement falseStatement)
  {
    Guard = guard;
    TrueStatement = trueStatement;
    FalseStatement = falseStatement;
  }
  public override Func<Zen<dynamic>, Zen<dynamic>> ToZen()
  {
    return t => Zen.If(Guard.ToZen<bool>(), TrueStatement.ToZen()(t), FalseStatement.ToZen()(t));
  }

  public override State Evaluate(State state)
  {
    var trueState = TrueStatement.Evaluate(state);
    var falseState = FalseStatement.Evaluate(state);
    trueState.Join(falseState, Guard.Evaluate(state));
    return trueState;
  }
}

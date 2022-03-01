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

  public override Dictionary<string, dynamic> Evaluate(Dictionary<string, dynamic> state)
  {
    var trueState = TrueStatement.Evaluate(state);
    var falseState = FalseStatement.Evaluate(state);
    // TODO: merge the resulting states for each variable according to the guard
    throw new NotImplementedException();
  }
}

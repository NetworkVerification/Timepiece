using ZenLib;

namespace Gardener.AstExpr;

public class First<TA, TB, TState> : Expr<TA, TState>
{
  public First(Expr<Pair<TA, TB>, TState> pair)
  {
    Pair = pair;
  }

  private Expr<Pair<TA, TB>, TState> Pair { get; set; }
  public override Func<Zen<TState>, Zen<TA>> Evaluate(State<TState> state)
  {
    var f = Pair.Evaluate(state);
    return s => f(s).Item1();
  }

  public override void Rename(string oldVar, string newVar)
  {
    Pair.Rename(oldVar, newVar);
  }
}

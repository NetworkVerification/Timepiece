using ZenLib;

namespace Gardener.AstExpr;

public class Second<TA, TB, TState> : Expr<TB, TState>
{
  public Second(Expr<Pair<TA, TB>, TState> pair)
  {
    Pair = pair;
  }

  private Expr<Pair<TA, TB>, TState> Pair { get; set; }
  public override Func<Zen<TState>, Zen<TB>> Evaluate(State<TState> state)
  {
    return s => Pair.Evaluate(state)(s).Item2();
  }

  public override void Rename(string oldVar, string newVar)
  {
    Pair.Rename(oldVar, newVar);
  }
}

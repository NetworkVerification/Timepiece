using ZenLib;

namespace Gardener.AstExpr;

public class PairExpr<TA, TB, T> : Expr<Pair<TA, TB>, T>
{
  private readonly Expr<TA, T> _first;
  private readonly Expr<TB, T> _second;

  public PairExpr(Expr<TA, T> first, Expr<TB, T> second)
  {
    _first = first;
    _second = second;
  }

  public PairExpr<TA, TB, T> WithSecond(Expr<TB, T> snd)
  {
    return new PairExpr<TA, TB, T>(_first, snd);
  }

  public override Func<Zen<T>, Zen<Pair<TA, TB>>> Evaluate(State<T> state)
  {
    return r => Pair.Create(_first.Evaluate(state)(r), _second.Evaluate(state)(r));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _first.Rename(oldVar, newVar);
    _second.Rename(oldVar, newVar);
  }
}

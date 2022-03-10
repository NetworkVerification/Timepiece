using ZenLib;

namespace Gardener;

public class Havoc<TState> : Expr<bool, TState>
{
  public override Func<Zen<TState>, Zen<bool>> Evaluate(State<TState> state)
  {
    return _ => Zen.Symbolic<bool>();
  }
}

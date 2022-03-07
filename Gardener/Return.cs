using ZenLib;

namespace Gardener;

public class Return<T>: Statement<T>
{
  public Return(Expr<T> expr)
  {
    Expr = expr;
  }

  public Expr<T> Expr { get; set; }
  public override State Evaluate(State state)
  {
    state.Return = Expr.Evaluate<T>(state);
    return state;
  }

  public override Statement<Unit> Bind(string var)
  {
    return new Assign<T>(var, Expr);
  }
}

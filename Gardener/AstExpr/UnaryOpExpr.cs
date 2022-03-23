using ZenLib;

namespace Gardener.AstExpr;

public class UnaryOpExpr<TArg, TResult, TState> : Expr<TResult, TState>
{
  private readonly Func<Zen<TArg>,Zen<TResult>> _unaryOp;
  private readonly Expr<TArg,TState> _e;

  internal UnaryOpExpr(Expr<TArg, TState> e, Func<Zen<TArg>, Zen<TResult>> unaryOp)
  {
    _e = e;
    _unaryOp = unaryOp;
  }
  public override Func<Zen<TState>, Zen<TResult>> Evaluate(AstState<TState> astState)
  {
    var f = _e.Evaluate(astState);
    return t => _unaryOp(f(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _e.Rename(oldVar, newVar);
  }
}

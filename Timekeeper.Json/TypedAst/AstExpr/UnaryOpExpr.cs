using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class UnaryOpExpr<TArg, TResult, TState> : Expr<TResult, TState>
{
  private readonly Expr<TArg, TState> _expr;
  private readonly Func<Zen<TArg>, Zen<TResult>> _unaryOp;

  internal UnaryOpExpr(Expr<TArg, TState> expr, Func<Zen<TArg>, Zen<TResult>> unaryOp)
  {
    _expr = expr;
    _unaryOp = unaryOp;
  }

  public override Func<Zen<TState>, Zen<TResult>> Evaluate(AstState<TState> astState)
  {
    var f = _expr.Evaluate(astState);
    return t => _unaryOp(f(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _expr.Rename(oldVar, newVar);
  }
}

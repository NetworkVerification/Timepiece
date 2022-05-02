using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class UnaryOpExpr<TArg, TResult> : Expr<TResult>
{
  private readonly Expr<TArg> _expr;
  private readonly Func<Zen<TArg>, Zen<TResult>> _unaryOp;

  internal UnaryOpExpr(Expr<TArg> expr, Func<Zen<TArg>, Zen<TResult>> unaryOp)
  {
    _expr = expr;
    _unaryOp = unaryOp;
  }

  public override Func<Zen<TS>, Zen<TResult>> Evaluate<TS>(AstState astState)
  {
    var f = _expr.Evaluate<TS>(astState);
    return t => _unaryOp(f(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _expr.Rename(oldVar, newVar);
  }
}

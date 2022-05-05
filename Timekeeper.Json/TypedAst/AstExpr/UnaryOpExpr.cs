using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class UnaryOpExpr<TArg, TResult> : Expr<TResult>
{
  public readonly Expr<TArg> expr;
  public readonly Func<Zen<TArg>, Zen<TResult>> unaryOp;

  internal UnaryOpExpr(Expr<TArg> expr, Func<Zen<TArg>, Zen<TResult>> unaryOp)
  {
    this.expr = expr;
    this.unaryOp = unaryOp;
  }

  public override Zen<TResult> Evaluate(AstState astState)
  {
    var t = expr.Evaluate(astState);
    return unaryOp(t);
  }

  public override void Rename(string oldVar, string newVar)
  {
    expr.Rename(oldVar, newVar);
  }
}

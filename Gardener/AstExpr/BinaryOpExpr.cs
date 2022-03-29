using Newtonsoft.Json;
using ZenLib;

namespace Gardener.AstExpr;

public class BinaryOpExpr<TArg1, TArg2, TResult, TState> : Expr<TResult, TState>
{
  private readonly Expr<TArg1, TState> _e1;
  private readonly Expr<TArg2, TState> _e2;
  private readonly Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> _binaryOp;

  internal BinaryOpExpr(Expr<TArg1, TState> e1, Expr<TArg2, TState> e2,
    Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> binaryOp)
  {
    _e1 = e1;
    _e2 = e2;
    _binaryOp = binaryOp;
  }

  public override Func<Zen<TState>, Zen<TResult>> Evaluate(AstState<TState> astState)
  {
    var f1 = _e1.Evaluate(astState);
    var f2 = _e2.Evaluate(astState);
    return t => _binaryOp(f1(t), f2(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _e1.Rename(oldVar, newVar);
    _e2.Rename(oldVar, newVar);
  }
}

public static class BinaryOpExprExtensions
{
  public static BinaryOpExpr<T, T, T, TState> Aggregate<T, TState>(this IEnumerable<Expr<T, TState>> es,
    Expr<T, TState> defaultExpr, Func<Zen<T>, Zen<T>, Zen<T>> binaryOp)
  {
    var operands = es.ToArray();
    if (operands.Length < 1)
    {
      throw new ArgumentException("Binary op expression given fewer than 2 elements.");
    }

    return new BinaryOpExpr<T, T, T, TState>(operands[0],
      operands.Length > 1 ? Aggregate(operands.Skip(1), defaultExpr, binaryOp) : defaultExpr, binaryOp);
  }
}

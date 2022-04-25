using ZenLib;

namespace Gardener.AstExpr;

public class BinaryOpExpr<TArg1, TArg2, TResult, TState> : Expr<TResult, TState>
{
  private readonly Expr<TArg1, TState> _expr1;
  private readonly Expr<TArg2, TState> _expr2;
  private readonly Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> _binaryOp;

  internal BinaryOpExpr(Expr<TArg1, TState> expr1, Expr<TArg2, TState> expr2,
    Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> binaryOp)
  {
    _expr1 = expr1;
    _expr2 = expr2;
    _binaryOp = binaryOp;
  }

  public override Func<Zen<TState>, Zen<TResult>> Evaluate(AstState<TState> astState)
  {
    var f1 = _expr1.Evaluate(astState);
    var f2 = _expr2.Evaluate(astState);
    return t => _binaryOp(f1(t), f2(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    _expr1.Rename(oldVar, newVar);
    _expr2.Rename(oldVar, newVar);
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

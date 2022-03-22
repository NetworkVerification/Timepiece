using ZenLib;

namespace Gardener.AstExpr;

public class BinaryOpExpr<TArg, TResult, TState> : Expr<TResult, TState>
{
  private readonly Expr<TArg, TState> _e1;
  private readonly Expr<TArg, TState> _e2;
  private readonly Func<Zen<TArg>, Zen<TArg>, Zen<TResult>> _binaryOp;

  internal BinaryOpExpr(Expr<TArg, TState> e1, Expr<TArg, TState> e2,
    Func<Zen<TArg>, Zen<TArg>, Zen<TResult>> binaryOp)
  {
    _e1 = e1;
    _e2 = e2;
    _binaryOp = binaryOp;
  }

  public override Func<Zen<TState>, Zen<TResult>> Evaluate(State<TState> state)
  {
    var f1 = _e1.Evaluate(state);
    var f2 = _e2.Evaluate(state);
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
  public static BinaryOpExpr<T, T, TState> Aggregate<T, TState>(this IEnumerable<Expr<T, TState>> es,
    Expr<T, TState> defaultExpr, Func<Zen<T>, Zen<T>, Zen<T>> binaryOp)
  {
    var operands = es.ToArray();
    if (operands.Length < 1)
    {
      throw new ArgumentException("Binary op expression given fewer than 2 elements.");
    }

    return new BinaryOpExpr<T, T, TState>(operands[0],
      operands.Length > 1 ? Aggregate(operands.Skip(1), defaultExpr, binaryOp) : defaultExpr, binaryOp);
  }
}

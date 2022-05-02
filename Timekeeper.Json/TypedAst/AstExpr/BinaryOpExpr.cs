using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class BinaryOpExpr<TArg1, TArg2, TResult> : Expr<TResult>
{
  private readonly Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> _binaryOp;
  private readonly Expr<TArg1> _expr1;
  private readonly Expr<TArg2> _expr2;

  internal BinaryOpExpr(Expr<TArg1> expr1, Expr<TArg2> expr2,
    Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> binaryOp)
  {
    _expr1 = expr1;
    _expr2 = expr2;
    _binaryOp = binaryOp;
  }

  public override Func<Zen<TS>, Zen<TResult>> Evaluate<TS>(AstState astState)
  {
    var f1 = _expr1.Evaluate<TS>(astState);
    var f2 = _expr2.Evaluate<TS>(astState);
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
  public static BinaryOpExpr<T, T, T> Aggregate<T>(this IEnumerable<Expr<T>> es,
    Expr<T> defaultExpr, Func<Zen<T>, Zen<T>, Zen<T>> binaryOp)
  {
    var operands = es.ToArray();
    if (operands.Length < 1) throw new ArgumentException("Binary op expression given fewer than 2 elements.");

    return new BinaryOpExpr<T, T, T>(operands[0],
      operands.Length > 1 ? Aggregate(operands.Skip(1), defaultExpr, binaryOp) : defaultExpr, binaryOp);
  }
}

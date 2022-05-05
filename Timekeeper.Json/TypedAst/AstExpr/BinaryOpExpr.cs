using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class BinaryOpExpr<TArg1, TArg2, TResult> : Expr<TResult>
{
  public readonly Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> binaryOp;
  public readonly Expr<TArg1> expr1;
  public readonly Expr<TArg2> expr2;

  internal BinaryOpExpr(Expr<TArg1> expr1, Expr<TArg2> expr2,
    Func<Zen<TArg1>, Zen<TArg2>, Zen<TResult>> binaryOp)
  {
    this.expr1 = expr1;
    this.expr2 = expr2;
    this.binaryOp = binaryOp;
  }

  public override Zen<TResult> Evaluate(AstState astState)
  {
    var t1 = expr1.Evaluate(astState);
    var t2 = expr2.Evaluate(astState);
    return binaryOp(t1, t2);
  }

  public override void Rename(string oldVar, string newVar)
  {
    expr1.Rename(oldVar, newVar);
    expr2.Rename(oldVar, newVar);
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

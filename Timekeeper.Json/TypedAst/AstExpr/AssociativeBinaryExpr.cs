using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class AssociativeBinaryExpr<TArg, TState> : BinaryOpExpr<TArg, TArg, TArg, TState>
{
  internal AssociativeBinaryExpr(Expr<TArg, TState> expr1, Expr<TArg, TState> expr2,
    Func<Zen<TArg>, Zen<TArg>, Zen<TArg>> binaryOp) : base(expr1, expr2, binaryOp)
  {
  }

  internal AssociativeBinaryExpr(IEnumerable<Expr<TArg, TState>> es, Expr<TArg, TState> identity,
    Func<Zen<TArg>, Zen<TArg>, Zen<TArg>> binaryOp) :
    this(identity, FromEnumerable(es, identity, binaryOp), binaryOp)
  {
  }

  private static AssociativeBinaryExpr<TArg, TState> FromEnumerable(IEnumerable<Expr<TArg, TState>> es,
    Expr<TArg, TState> identity, Func<Zen<TArg>, Zen<TArg>, Zen<TArg>> binaryOp)
  {
    var operands = es.ToArray();
    return operands.Length switch
    {
      0 => throw new ArgumentException("Invalid number of arguments to binary expression"),
      1 => new AssociativeBinaryExpr<TArg, TState>(operands[0], identity, binaryOp),
      _ => new AssociativeBinaryExpr<TArg, TState>(operands[0], FromEnumerable(operands[1..], identity, binaryOp),
        binaryOp)
    };
  }
}

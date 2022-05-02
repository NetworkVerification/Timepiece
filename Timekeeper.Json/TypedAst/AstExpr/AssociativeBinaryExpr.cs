using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class AssociativeBinaryExpr<TArg> : BinaryOpExpr<TArg, TArg, TArg>
{
  internal AssociativeBinaryExpr(Expr<TArg> expr1, Expr<TArg> expr2,
    Func<Zen<TArg>, Zen<TArg>, Zen<TArg>> binaryOp) : base(expr1, expr2, binaryOp)
  {
  }

  internal AssociativeBinaryExpr(IEnumerable<Expr<TArg>> es, Expr<TArg> identity,
    Func<Zen<TArg>, Zen<TArg>, Zen<TArg>> binaryOp) :
    this(identity, FromEnumerable(es, identity, binaryOp), binaryOp)
  {
  }

  private static AssociativeBinaryExpr<TArg> FromEnumerable(IEnumerable<Expr<TArg>> es,
    Expr<TArg> identity, Func<Zen<TArg>, Zen<TArg>, Zen<TArg>> binaryOp)
  {
    var operands = es.ToArray();
    return operands.Length switch
    {
      0 => throw new ArgumentException("Invalid number of arguments to binary expression"),
      1 => new AssociativeBinaryExpr<TArg>(operands[0], identity, binaryOp),
      _ => new AssociativeBinaryExpr<TArg>(operands[0], FromEnumerable(operands[1..], identity, binaryOp),
        binaryOp)
    };
  }
}

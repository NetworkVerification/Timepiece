using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Plus<T> : BinaryOpExpr
{
  public Plus(Expr expr1, Expr expr2) : base(expr1, expr2,
    new Func<Zen<T>, Zen<T>, Zen<T>>((e1, e2) => Zen.Plus(e1, e2)))
  {
  }
}

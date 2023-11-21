using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record PairExpr : BinaryOpExpr
{
  public PairExpr(Expr first, Expr second) : base(first, second,
    (e1, e2) => Pair.Create(e1, e2))
  {
  }
}

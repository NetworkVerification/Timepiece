using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record Second : UnaryOpExpr
{
  public Second(Expr pair) : base(pair, e => Pair.Item2(e))
  {
  }
}

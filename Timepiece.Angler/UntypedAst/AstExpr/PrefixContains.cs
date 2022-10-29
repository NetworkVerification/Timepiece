using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class PrefixContains : BinaryOpExpr
{
  public PrefixContains(Expr addr, Expr prefix) : base(addr, prefix,
    (a, p) => Zen.Symbolic<bool>())
  {
  }
}

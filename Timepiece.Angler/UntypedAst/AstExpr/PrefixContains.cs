using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class PrefixContains : BinaryOpExpr
{
  public PrefixContains(Expr addr, Expr prefix) : base(addr, prefix,
    // TODO: add in the correct behavior
    (_, _) => Zen.Symbolic<bool>())
  {
  }
}

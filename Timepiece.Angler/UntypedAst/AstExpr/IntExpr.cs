using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class IntExpr : ConstantExpr
{
  public IntExpr(int value) : base(value, v => Zen.Constant<int>(v))
  {
  }
}

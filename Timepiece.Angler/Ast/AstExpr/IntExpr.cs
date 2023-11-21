using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record IntExpr : ConstantExpr
{
  public IntExpr(int value) : base(value, v => Zen.Constant<int>(v))
  {
  }
}

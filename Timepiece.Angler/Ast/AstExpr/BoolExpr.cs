using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record BoolExpr : ConstantExpr
{
  public BoolExpr(bool value) : base(value, v => Zen.Constant<bool>(v))
  {
  }
}

using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record UIntExpr : ConstantExpr
{
  public UIntExpr(uint value) : base(value, v => Zen.Constant<uint>(v))
  {
  }
}

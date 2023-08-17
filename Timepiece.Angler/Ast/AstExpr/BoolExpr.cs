using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class BoolExpr : ConstantExpr
{
  public BoolExpr(bool value) : base(value, v => Zen.Constant<bool>(v))
  {
  }
}

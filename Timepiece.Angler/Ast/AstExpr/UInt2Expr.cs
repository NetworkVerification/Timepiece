using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record UInt2Expr : ConstantExpr
{
  public UInt2Expr(UInt<_2> value) : base(value, v => Zen.Constant<UInt<_2>>(v))
  {
  }

  [JsonConstructor]
  public UInt2Expr(int value) : this(new UInt<_2>(value))
  {
  }
}

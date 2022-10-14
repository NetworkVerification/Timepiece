using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class UInt2Expr : ConstantExpr
{
  public UInt2Expr(UInt<_2> value) : base(value)
  {
  }

  [JsonConstructor]
  public UInt2Expr(int value) : this(new UInt<_2>(value))
  {
  }
}

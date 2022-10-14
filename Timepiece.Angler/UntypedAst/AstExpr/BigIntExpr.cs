using System.Numerics;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class BigIntExpr : ConstantExpr
{
  public BigIntExpr(BigInteger value) : base(value)
  {
  }
}

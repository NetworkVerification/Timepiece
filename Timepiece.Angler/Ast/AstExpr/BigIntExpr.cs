using System.Numerics;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class BigIntExpr : ConstantExpr
{
  public BigIntExpr(BigInteger value) : base(value, v => Zen.Constant<BigInteger>(v))
  {
  }
}
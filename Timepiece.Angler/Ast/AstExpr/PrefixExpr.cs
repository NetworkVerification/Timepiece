using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record PrefixExpr : ConstantExpr
{
  public PrefixExpr(Ipv4Prefix prefix) : base(prefix, v => Zen.Constant<Ipv4Prefix>(v))
  {
  }
}

using Timepiece.Datatypes;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class PrefixExpr : ConstantExpr
{
  public PrefixExpr(Ipv4Prefix prefix) : base(prefix, v => Zen.Constant<Ipv4Prefix>(v))
  {
  }
}

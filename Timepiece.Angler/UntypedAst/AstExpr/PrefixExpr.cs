using Timepiece.Datatypes;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class PrefixExpr : ConstantExpr
{
  public PrefixExpr(Ipv4Prefix value) : base(value)
  {
  }
}

using Timepiece.DataTypes;

namespace Timepiece.Angler.Ast.AstExpr;

public record PrefixContains : BinaryOpExpr
{
  public PrefixContains(Expr address, Expr prefix) : base(address, prefix,
    (a, p) => Ipv4WildcardExtensions.ContainsIp(p, a))
  {
  }
}

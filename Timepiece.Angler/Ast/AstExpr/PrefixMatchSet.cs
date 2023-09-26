namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
/// An expression that returns true if the second operand (the set)
/// contains the first operand (the prefix).
/// </summary>
public class PrefixMatchSet : BinaryOpExpr
{
  public PrefixMatchSet(Expr prefix, Expr prefixSet) : base(prefix, prefixSet,
    (p, pSet) => pSet.Contains(p))
  {
  }
}

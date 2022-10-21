namespace Timepiece.Angler.UntypedAst.AstExpr;

public class ConjunctionChain : VariadicExpr
{
  public ConjunctionChain(params Expr[] subroutines) : base(subroutines)
  {
  }
}

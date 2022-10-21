namespace Timepiece.Angler.UntypedAst.AstExpr;

public class FirstMatchChain : VariadicExpr
{
  public FirstMatchChain(params Expr[] subroutines) : base(subroutines)
  {
  }
}

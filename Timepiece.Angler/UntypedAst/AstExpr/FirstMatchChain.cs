namespace Timepiece.Angler.UntypedAst.AstExpr;

public class FirstMatchChain : Expr
{
  public List<Expr> Subroutines { get; set; }

  public FirstMatchChain(List<Expr> subroutines)
  {
    Subroutines = subroutines;
  }

  public override void Rename(string oldVar, string newVar)
  {
    foreach (var subroutine in Subroutines)
    {
      subroutine.Rename(oldVar, newVar);
    }
  }
}

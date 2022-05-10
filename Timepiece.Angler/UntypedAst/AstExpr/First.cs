namespace Timepiece.Angler.UntypedAst.AstExpr;

public class First : UnaryOpExpr
{
  public First(Expr expr) : base(expr, e => e.Item1())
  {
  }
}

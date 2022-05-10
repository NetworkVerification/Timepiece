namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Second : UnaryOpExpr
{
  public Second(Expr expr) : base(expr, e => e.Item2())
  {
  }
}

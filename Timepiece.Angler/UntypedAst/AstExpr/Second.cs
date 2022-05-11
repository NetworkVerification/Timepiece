namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Second : UnaryOpExpr
{
  // FIXME: need to specify type arguments to Item2
  public Second(Expr pair) : base(pair, e => e.Item2())
  {
  }
}

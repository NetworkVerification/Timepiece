namespace Timepiece.Angler.UntypedAst.AstExpr;

public class First : UnaryOpExpr
{
  // FIXME: need to specify type arguments to Item1
  public First(Expr pair) : base(pair, e => e.Item1())
  {
  }
}

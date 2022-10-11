using Xunit.Sdk;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class LiteralSet : Expr
{
  public readonly dynamic[] exprs;
  public LiteralSet(dynamic[] exprs)
  {
    this.exprs = exprs;
  }

  public static LiteralSet Empty()
  {
    return new LiteralSet(Array.Empty<dynamic>());
  }
  public override void Rename(string oldVar, string newVar)
  {
    ;
  }
}

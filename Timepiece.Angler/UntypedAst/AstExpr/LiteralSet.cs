namespace Timepiece.Angler.UntypedAst.AstExpr;

public class LiteralSet : Expr
{
  public readonly dynamic[] elements;

  public LiteralSet(dynamic[] elements)
  {
    this.elements = elements;
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

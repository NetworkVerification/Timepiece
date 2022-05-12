using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class First : UnaryOpExpr
{
  private static GenericMethod GetMethod(Type type1, Type type2) => new(typeof(Pair), "Item1", type1, type2);

  public First(Type firstType, Type secondType, Expr pair) : base(pair,
    e => GetMethod(firstType, secondType).Call(e))
  {
  }
}

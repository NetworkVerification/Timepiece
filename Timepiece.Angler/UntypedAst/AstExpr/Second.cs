using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Second : UnaryOpExpr
{
  private static GenericMethod GetMethod(Type type1, Type type2) => new(typeof(Pair), "Item2", type1, type2);

  public Second(Type firstType, Type secondType, Expr pair) : base(pair, e => Pair.Item2(e))
  {
  }

  [JsonConstructor]
  public Second(string firstType, string secondType, Expr pair) : this(TypeParsing.ParseType(firstType).MakeType(),
    TypeParsing.ParseType(secondType).MakeType(), pair)
  {
  }
}

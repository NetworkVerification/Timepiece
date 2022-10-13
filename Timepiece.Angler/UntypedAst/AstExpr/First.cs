using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class First : UnaryOpExpr
{
  [JsonConstructor]
  public First(Expr pair) : base(pair,
    e => Pair.Item1(e))
  {
  }
}

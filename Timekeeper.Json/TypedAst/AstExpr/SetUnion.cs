using Newtonsoft.Json;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class SetUnion : AssociativeBinaryExpr<Set<string>>
{
  public SetUnion(Expr<Set<string>> expr1, Expr<Set<string>> expr2) : base(expr1, expr2, Set.Union)
  {
  }

  [JsonConstructor]
  public SetUnion(IEnumerable<Expr<Set<string>>> sets) : base(sets, new EmptySet(), Set.Union)
  {
  }
}

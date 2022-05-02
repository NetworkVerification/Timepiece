using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class SetContains : BinaryOpExpr<string, Set<string>, bool>
{
  public SetContains(Expr<string> search, Expr<Set<string>> set) : base(search, set,
    (s, st) => st.Contains(s))
  {
  }
}

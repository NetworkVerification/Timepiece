using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class SetAdd : BinaryOpExpr<string, Set<string>, Set<string>>
{
  public SetAdd(Expr<string> expr, Expr<Set<string>> set) : base(expr, set,
    (s, st) => st.Add(s))
  {
  }
}

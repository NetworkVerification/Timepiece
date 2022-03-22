using Newtonsoft.Json;
using ZenLib;

namespace Gardener.AstExpr;

public class Or<T> : BinaryOpExpr<bool, bool, bool, T>
{
  public Or(Expr<bool, T> e1, Expr<bool, T> e2) : base(e1, e2, Zen.Or) { }
}

using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class ConstantExpr<T> : Expr<T>
{
  public readonly T value;

  public ConstantExpr(T value)
  {
    this.value = value;
  }

  public override Func<Zen<TS>, Zen<T>> Evaluate<TS>(AstState astState)
  {
    return _ => Zen.Constant(value);
  }

  public override void Rename(string oldVar, string newVar)
  {
  }
}

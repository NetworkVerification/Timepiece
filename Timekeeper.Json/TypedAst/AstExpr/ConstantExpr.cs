using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class ConstantExpr<T> : Expr<T>
{
  private readonly T _value;

  public ConstantExpr(T value)
  {
    _value = value;
  }

  public override Func<Zen<TS>, Zen<T>> Evaluate<TS>(AstState astState)
  {
    return _ => Zen.Constant(_value);
  }

  public override void Rename(string oldVar, string newVar)
  {
  }
}

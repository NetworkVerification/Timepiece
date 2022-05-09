using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class ConstantExpr<T> : Expr<T>
{
  public readonly T value;

  public ConstantExpr(T value)
  {
    this.value = value;
  }

  public override Zen<T> Evaluate(AstState astState)
  {
    return Zen.Constant(value);
  }

  public override void Rename(string oldVar, string newVar)
  {
  }
}

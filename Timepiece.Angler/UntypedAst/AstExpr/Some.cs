using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Some : UnaryOpExpr
{
  public Some(Type innerTy, Expr expr) : base(expr, Op(innerTy))
  {
  }

  private static Func<dynamic, object> Op(Type innerTy)
  {
    return v =>
      typeof(Option).GetMethod("Create")!.MakeGenericMethod(innerTy).Invoke(null, new[] {v})!;
  }
}

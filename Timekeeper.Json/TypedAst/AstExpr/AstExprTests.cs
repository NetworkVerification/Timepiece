using Xunit;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public static class AstExprTests
{
  [Fact]
  public static void TestBinaryOpExprs()
  {
    var e1 = new And(new ConstantExpr<bool>(true), new ConstantExpr<bool>(false));
    var e2 = new And(new ConstantExpr<bool>(true), new Not(new ConstantExpr<bool>(false)));
    var e3 = new Or(e1, e2);
    var b = e3.Evaluate(new AstState());
    var model = b.Solve();
    Assert.True(model.IsSatisfiable());
  }

  [Fact]
  public static void TestAssociativeBinaryExprs()
  {
    var e1 = new And(new[] {new ConstantExpr<bool>(true)});
    var e2 = new Or(new Expr<bool>[]
      {new ConstantExpr<bool>(true), new ConstantExpr<bool>(false), e1});
    var b = e2.Evaluate(new AstState());
    var model = b.Solve();
    Assert.True(model.IsSatisfiable());
  }
}

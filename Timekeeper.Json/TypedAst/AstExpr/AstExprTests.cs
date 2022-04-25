using Xunit;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public static class AstExprTests
{
  [Fact]
  public static void TestBinaryOpExprs()
  {
    var e1 = new And<bool>(new ConstantExpr<bool, bool>(true), new ConstantExpr<bool, bool>(false));
    var e2 = new And<bool>(new ConstantExpr<bool, bool>(true), new Not<bool>(new ConstantExpr<bool, bool>(false)));
    var e3 = new Or<bool>(e1, e2);
    var f = e3.Evaluate(new AstState<bool>());
    var model = f(Zen.False()).Solve();
    Assert.True(model.IsSatisfiable());
  }

  [Fact]
  public static void TestAssociativeBinaryExprs()
  {
    var e1 = new And<bool>(new[] {new ConstantExpr<bool, bool>(true)});
    var e2 = new Or<bool>(new Expr<bool, bool>[]
      {new ConstantExpr<bool, bool>(true), new ConstantExpr<bool, bool>(false), e1});
    var f = e2.Evaluate(new AstState<bool>());
    var model = f(Zen.False()).Solve();
    Assert.True(model.IsSatisfiable());
  }
}

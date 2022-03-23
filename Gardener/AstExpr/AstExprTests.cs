using Xunit;
using ZenLib;

namespace Gardener.AstExpr;

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
}

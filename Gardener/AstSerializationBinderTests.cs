using Gardener.AstExpr;
using Gardener.AstStmt;
using Xunit;
using ZenLib;

namespace Gardener;

using ZenRoute = Pair<bool, BatfishBgpRoute>;
using AstRoute = PairExpr<bool, BatfishBgpRoute, Pair<bool, BatfishBgpRoute>>;

public static class AstSerializationBinderTests
{
  /// <summary>
  /// The binder to use for the tests.
  /// </summary>
  private static readonly AstSerializationBinder<BatfishBgpRoute, ZenRoute> Binder = new();

  [Theory]
  [InlineData("TRoute", typeof(BatfishBgpRoute))]
  [InlineData("TBool", typeof(bool))]
  [InlineData("Pair(TBool;TRoute)", typeof(AstRoute))]
  [InlineData("Return(TPair(TBool;TRoute))", typeof(Return<ZenRoute>))]
  [InlineData("Var(TPair(TBool;TRoute))", typeof(Var<ZenRoute>))]
  [InlineData("If(TPair(TBool;TRoute))", typeof(IfThenElse<ZenRoute, ZenRoute>))]
  public static void BindToTypeAuxReturnsCorrectType(string typeName, Type expectedType)
  {
    var t = Binder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }
}

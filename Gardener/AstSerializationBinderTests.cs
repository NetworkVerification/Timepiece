using Gardener.AstExpr;
using Gardener.AstStmt;
using Xunit;
using ZenLib;

namespace Gardener;

using Route = Pair<bool, BatfishBgpRoute>;

public static class AstSerializationBinderTests
{
  /// <summary>
  /// The binder to use for the tests.
  /// </summary>
  private static readonly AstSerializationBinder<BatfishBgpRoute> _binder = new();

  [Theory]
  [InlineData("Route", typeof(BatfishBgpRoute))]
  [InlineData("Bool", typeof(bool))]
  [InlineData("Return(Route)", typeof(Return<Route>))]
  [InlineData("Pair(Bool;Route)", typeof(PairExpr<bool, BatfishBgpRoute, Route>))]
  [InlineData("Return(Pair(Bool;Route))", typeof(Return<Route>))]
  public static void BindToTypeAuxReturnsCorrectType(string typeName, Type expectedType)
  {
    var t = _binder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }
}

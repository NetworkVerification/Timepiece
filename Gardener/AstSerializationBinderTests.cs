using System.Numerics;
using Gardener.AstExpr;
using Gardener.AstStmt;
using Xunit;
using ZenLib;

namespace Gardener;

using ZenRoute = Pair<bool, BatfishBgpRoute>;

public static class AstSerializationBinderTests
{
  /// <summary>
  /// The binder to use for the tests without time.
  /// </summary>
  private static readonly AstSerializationBinder<BatfishBgpRoute, ZenRoute> AtemporalBinder = new();
  private static readonly AstSerializationBinder<BatfishBgpRoute, Pair<ZenRoute, BigInteger>> TemporalBinder = new();

  [Theory]
  [InlineData("TRoute", typeof(BatfishBgpRoute))]
  [InlineData("TBool", typeof(bool))]
  [InlineData("Pair(TBool;TRoute)", typeof(PairExpr<bool, BatfishBgpRoute, ZenRoute>))]
  [InlineData("Return(TPair(TBool;TRoute))", typeof(Return<ZenRoute>))]
  [InlineData("Var(TPair(TBool;TRoute))", typeof(Var<ZenRoute>))]
  [InlineData("If(TPair(TBool;TRoute))", typeof(IfThenElse<ZenRoute, ZenRoute>))]
  [InlineData("First(TBool;TRoute)", typeof(First<bool, BatfishBgpRoute, ZenRoute>))]
  public static void BindToTypeReturnsCorrectType(string typeName, Type expectedType)
  {
    var t = AtemporalBinder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }

  [Theory]
  [InlineData("Pair(TPair(TBool;TRoute);TTime)",
    typeof(PairExpr<ZenRoute, BigInteger, Pair<ZenRoute, BigInteger>>))]
  public static void BindToTypeReturnsCorrectTypeTemporal(string typeName, Type expectedType)
  {
    var t = TemporalBinder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }
}

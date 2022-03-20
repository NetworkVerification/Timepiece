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
  private static readonly AstSerializationBinder<BatfishBgpRoute, ZenRoute> Binder = new();

  [Theory]
  [InlineData("TRoute", typeof(BatfishBgpRoute))]
  [InlineData("TBool", typeof(bool))]
  [InlineData("Pair(TBool;TRoute)", typeof(PairExpr<bool, BatfishBgpRoute, ZenRoute>))]
  [InlineData("Return(TPair(TBool;TRoute))", typeof(Return<ZenRoute>))]
  [InlineData("Var(TPair(TBool;TRoute))", typeof(Var<ZenRoute>))]
  [InlineData("If(TPair(TBool;TRoute))", typeof(IfThenElse<ZenRoute, ZenRoute>))]
  [InlineData("First(TBool;TRoute)", typeof(First<bool, BatfishBgpRoute, ZenRoute>))]
  [InlineData("TPair(TPair(TBool;TInt32);TUnit", typeof(Pair<Pair<bool, int>, Unit>))]
  [InlineData("Pair(TPair(TBool;TRoute);TInt32)", typeof(PairExpr<Pair<bool, BatfishBgpRoute>, int, ZenRoute>))]
  [InlineData("!Pair(TPair(TBool;TRoute);TTime)",
    typeof(PairExpr<ZenRoute, BigInteger, Pair<ZenRoute, BigInteger>>))]
  [InlineData("!Var(TPair(TPair(TBool;TRoute);TTime)", typeof(Var<Pair<ZenRoute, BigInteger>>))]
  public static void BindToTypeReturnsCorrectType(string typeName, Type expectedType)
  {
    var t = Binder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }
}

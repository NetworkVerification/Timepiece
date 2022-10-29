using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using Xunit;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public static class AstSerializationBinderTests
{
  /// <summary>
  ///   The binder to use for the tests without time.
  /// </summary>
  private static readonly AstSerializationBinder Binder = new();

  [Theory]
  [InlineData("TRoute", typeof(BatfishBgpRoute))]
  [InlineData("TBool", typeof(bool))]
  [InlineData("TPair(TBool;TRoute)", typeof(Pair<bool, BatfishBgpRoute>))]
  [InlineData("Pair(TBool;TRoute)", typeof(PairExpr))]
  [InlineData("Var(TPair(TBool;TRoute))", typeof(Var))]
  [InlineData("If(TPair(TBool;TRoute))", typeof(IfThenElse))]
  [InlineData("First(TBool;TRoute)", typeof(First))]
  [InlineData("TPair(TPair(TBool;TInt32);TUnit", typeof(Pair<Pair<bool, int>, Unit>))]
  [InlineData("Pair(TPair(TBool;TRoute);TInt32)", typeof(PairExpr))]
  [InlineData("Pair(TPair(TBool;TRoute);TTime)", typeof(PairExpr))]
  [InlineData("Var(TPair(TPair(TBool;TRoute);TTime)", typeof(Var))]
  public static void BindToTypeReturnsCorrectType(string typeName, Type expectedType)
  {
    var t = Binder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }
}

using Timepiece.Angler.Ast;
using Timepiece.Angler.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Tests;

public static class AstSerializationBinderTests
{
  /// <summary>
  ///   The binder to use for the tests without time.
  /// </summary>
  private static readonly AstSerializationBinder Binder = new();

  [Theory]
  [InlineData("TEnvironment", typeof(RouteEnvironment))]
  [InlineData("TBool", typeof(bool))]
  [InlineData("TPair(TBool;TEnvironment)", typeof(Pair<bool, RouteEnvironment>))]
  [InlineData("Pair(TBool;TEnvironment)", typeof(PairExpr))]
  [InlineData("Var(TPair(TBool;TEnvironment))", typeof(Var))]
  [InlineData("If(TPair(TBool;TEnvironment))", typeof(IfThenElse))]
  [InlineData("First(TBool;TEnvironment)", typeof(First))]
  [InlineData("TPair(TPair(TBool;TInt32);TUnit", typeof(Pair<Pair<bool, int>, Unit>))]
  [InlineData("Pair(TPair(TBool;TEnvironment);TInt32)", typeof(PairExpr))]
  [InlineData("Pair(TPair(TBool;TEnvironment);TTime)", typeof(PairExpr))]
  [InlineData("Var(TPair(TPair(TBool;TEnvironment);TTime)", typeof(Var))]
  public static void BindToTypeReturnsCorrectType(string typeName, Type expectedType)
  {
    var t = Binder.BindToType(null, typeName);
    Assert.Equal(expectedType, t);
  }
}

using System.Numerics;
using Timekeeper.Json.TypedAst.AstExpr;
using Timekeeper.Json.TypedAst.AstStmt;
using Xunit;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstFunction;

public static class AstFunctionTests
{
  [Fact]
  public static void TestHavoc()
  {
    var one = new ConstantExpr<BigInteger, BatfishBgpRoute>(0);
    var rVar = new Var<BatfishBgpRoute>("route");
    // AST representation of incrementing AsPathLength by 1
    var increment = new WithField<BatfishBgpRoute, BigInteger, BatfishBgpRoute>(
      rVar, "AsPathLength",
      new Plus<BigInteger, BatfishBgpRoute>(
        new GetField<BatfishBgpRoute, BigInteger, BatfishBgpRoute>(rVar, "AsPathLength"),
        one)
    );
    var r = Zen.Symbolic<BatfishBgpRoute>();
    var rIncremented = r.WithAsPathLength(r.GetAsPathLength() + BigInteger.One);
    var f = new AstFunction<BatfishBgpRoute>("route",
      new IfThenElse<BatfishBgpRoute, BatfishBgpRoute>(new Havoc<BatfishBgpRoute>(),
        new Return<BatfishBgpRoute>(increment),
        new Return<BatfishBgpRoute>(rVar)));
    var zenF = f.Evaluate(new AstState<BatfishBgpRoute>());
    // since the if is a havoc, we have that zenF(r) is either incremented or the same:
    var model = Zen.Not(Zen.And(Zen.Eq(zenF(r), r), Zen.Eq(zenF(r), rIncremented))).Solve();
    Assert.True(model.IsSatisfiable());
  }

  [Fact]
  public static void TestRename()
  {
    var f1 = new AstFunction<int>("x", new Seq<int, int>(new IfThenElse<Unit, int>(new Havoc<int>(),
      new Assign<int>("x", new Plus<int, int>(new Var<int>("x"), new ConstantExpr<int, int>(1))),
      new Assign<int>("x", new ConstantExpr<int, int>(0))), new Return<int>(new Var<int>("x"))));
    var f2 = new AstFunction<int>("x",
      new Return<int>(new Plus<int, int>(new Var<int>("x"), new ConstantExpr<int, int>(3))));
    f1.Rename("x", "y");
    var f = f1.Compose(f2).Evaluate(new AstState<int>());
    var x = Zen.Symbolic<int>();
    var model = Zen.Eq(f(x), x + 4).Solve();
    Assert.True(model.IsSatisfiable());
  }
}
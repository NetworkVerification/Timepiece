using Gardener.AstExpr;
using Gardener.AstStmt;
using Xunit;
using ZenLib;

namespace Gardener;

public static class AstFuncTests
{

  [Fact]
  public static void TestHavoc()
  {
    var one = new ConstantExpr<int, BatfishBgpRoute>(0);
    var rVar = new Var<BatfishBgpRoute>("route");
    // AST representation of incrementing AsPathLength by 1
    var increment = new WithField<BatfishBgpRoute, int, BatfishBgpRoute>(
      rVar, "AsPathLength",
      new Plus<int, BatfishBgpRoute>(
        new GetField<BatfishBgpRoute, int, BatfishBgpRoute>(rVar, "AsPathLength"),
        one)
    );
    var r = Zen.Symbolic<BatfishBgpRoute>();
    var rIncremented = r.WithAsPathLength(r.GetAsPathLength() + 1);
    var f = new AstFunction<BatfishBgpRoute>("route",
      new IfThenElse<BatfishBgpRoute, BatfishBgpRoute>(new Havoc<BatfishBgpRoute>(),
        new Return<BatfishBgpRoute>(increment),
        new Return<BatfishBgpRoute>(rVar)));
    var zenF = f.Evaluate(new State<BatfishBgpRoute>());
    // since the if is a havoc, we have that zenF(r) is either incremented or the same:
    var model = Zen.Not(Zen.And(Zen.Eq(zenF(r), r), Zen.Eq(zenF(r), rIncremented))).Solve();
    Assert.True(model.IsSatisfiable());
  }
}

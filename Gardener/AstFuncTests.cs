using Xunit;
using ZenLib;

namespace Gardener;

public static class AstFuncTests
{
  public static void Tests()
  {
    // var one = new IntExpr<int, Signed, BatfishBgpRoute>(new ZenLib.Int32(0));
    // var two = new Plus<int, Signed, BatfishBgpRoute>(one, one);
    var rVar = new Var<BatfishBgpRoute>("route");
    var increment = new WithField<BatfishBgpRoute, IntN<int, Signed>, BatfishBgpRoute>(
      rVar, "AsPathLength",
      new GetField<BatfishBgpRoute, IntN<int, Signed>, BatfishBgpRoute>(rVar, "AsPathLength")
    );
    var f = new AstFunc<BatfishBgpRoute>("route",
      new IfThenElse<BatfishBgpRoute, BatfishBgpRoute>(new Havoc<BatfishBgpRoute>(),
        new Return<BatfishBgpRoute>(increment),
        new Return<BatfishBgpRoute>(rVar)));
    var r = Zen.Symbolic<BatfishBgpRoute>();
    Assert.Equal(r, f.Evaluate(new State<BatfishBgpRoute>())(r));
  }
}

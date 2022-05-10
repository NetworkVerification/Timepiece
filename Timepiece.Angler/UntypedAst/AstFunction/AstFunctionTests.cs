using System.Numerics;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using Xunit;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

public static class AstFunctionTests
{
  [Fact]
  public static void TestHavoc()
  {
    const string pathLen = "AsPathLength";
    const string route = "route";
    var rVar = new Var(route);
    // AST representation of incrementing AsPathLength by 1
    var increment = new WithField(typeof(BatfishBgpRoute), typeof(BigInteger),
      rVar, pathLen,
      new Plus(
        new GetField(typeof(BatfishBgpRoute), typeof(BigInteger), rVar, pathLen),
        new ConstantExpr(BigInteger.One))
    );
    var r = Zen.Symbolic<BatfishBgpRoute>();
    var rIncremented = r.IncrementAsPathLength(BigInteger.One);
    var f = new AstFunction<BatfishBgpRoute>(route, new Statement[]
    {
      new IfThenElse(new Havoc(), new[] {new Return(increment)}, new[] {new Return(rVar)})
    });
    var zenF = f.Evaluate();
    // since the if is a havoc, we have that zenF(r) is either incremented or the same:
    var model = Zen.Not(Zen.And(Zen.Eq(zenF(r), r), Zen.Eq(zenF(r), rIncremented))).Solve();
    Assert.True(model.IsSatisfiable());
  }

  [Fact]
  public static void TestRename()
  {
    const string oldArg = "x";
    var f1 = new AstFunction<int>(oldArg, new Statement[]
    {
      new IfThenElse(new Havoc(),
        new[] {new Assign(oldArg, new Plus(new Var(oldArg), new ConstantExpr(1)))},
        new[] {new Assign(oldArg, new ConstantExpr(0))}),
      new Return(new Var(oldArg))
    });
    var f2 = new AstFunction<int>(oldArg, new[]
    {
      new Return(new Plus(new Var(oldArg), new ConstantExpr(3)))
    });
    f1.Rename(oldArg, "y");
    var f = f1.Compose(f2).Evaluate();
    var x = Zen.Symbolic<int>();
    var model = Zen.Eq(f(x), x + 4).Solve();
    Assert.True(model.IsSatisfiable());
  }
}

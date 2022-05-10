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
    var one = new ConstantExpr(0);
    var rVar = new Var("route");
    // AST representation of incrementing AsPathLength by 1
    var increment = new WithField(
      rVar, "AsPathLength",
      new Plus<BigInteger>(
        new GetField(typeof(BatfishBgpRoute), typeof(BigInteger), rVar, "AsPathLength"),
        one)
    );
    var r = Zen.Symbolic<BatfishBgpRoute>();
    var rIncremented = r.WithAsPathLength(r.GetAsPathLength() + BigInteger.One);
    var f = new AstFunction<BatfishBgpRoute>("route", new Statement[]
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
    var f1 = new AstFunction<int>("x", new Statement[]
    {
      new IfThenElse(new Havoc(),
        new[] {new Assign("x", new Plus<int>(new Var("x"), new ConstantExpr(1)))},
        new[] {new Assign("x", new ConstantExpr(0))}),
      new Return(new Var("x"))
    });
    var f2 = new AstFunction<int>("x", new[]
    {
      new Return(new Plus<int>(new Var("x"), new ConstantExpr(3)))
    });
    f1.Rename("x", "y");
    var f = f1.Compose(f2).Evaluate();
    var x = Zen.Symbolic<int>();
    var model = Zen.Eq(f(x), x + 4).Solve();
    Assert.True(model.IsSatisfiable());
  }
}

using System.Collections.Immutable;
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
    var increment = new WithField(rVar, pathLen,
      new Plus(
        new GetField(typeof(BatfishBgpRoute), typeof(BigInteger), rVar, pathLen),
        new BigIntExpr(BigInteger.One))
    );
    var r = Zen.Symbolic<BatfishBgpRoute>();
    var rIncremented = r.IncrementAsPathLength(BigInteger.One);
    var f = new AstFunction<BatfishBgpRoute>(route, new Statement[]
    {
      new IfThenElse(new Havoc(), new[] {new Assign(route, increment)}, new[] {new Assign(route, rVar)})
    });
    var zenF = f.Evaluate(new AstEnvironment());
    // since the if is a havoc, we have that zenF(r) is either incremented or the same:
    var model = Zen.Not(Zen.And(Zen.Eq(zenF(r), r), Zen.Eq(zenF(r), rIncremented))).Solve();
    Assert.True(model.IsSatisfiable());
  }

  [Fact]
  public static void TestRename()
  {
    const string oldArg = "x";
    // randomly choose to increment the argument or set it to 0
    var f1 = new AstFunction<int>(oldArg, new Statement[]
    {
      new IfThenElse(new Havoc(),
        new[] {new Assign(oldArg, new Plus(new Var(oldArg), new IntExpr(1)))},
        new[] {new Assign(oldArg, new IntExpr(0))}),
      new Assign(oldArg, new Var(oldArg))
    });
    // return the argument with 3 added to it
    var f2 = new AstFunction<int>(oldArg, new[]
    {
      new Assign(oldArg, new Plus(new Var(oldArg), new IntExpr(3)))
    });
    f1.Rename(oldArg, "y");
    var f = new Func<Zen<int>, Zen<int>>(t => f2.Evaluate(new AstEnvironment())(f1.Evaluate(new AstEnvironment())(t)));
    var x = Zen.Symbolic<int>();
    var y = Zen.Symbolic<int>();
    // check that there does not exist a model where f(x) == y and y != x + 4 and y != 3
    // note that we need to store the result of f(x) in y as separate calls to f(x) can choose
    // to branch differently, meaning f(x) == 3 and f(x) != 3 is satisfiable (pick the else branch, pick the then branch)
    var model = Zen.Not(Zen.Implies(Zen.Eq(f(x), y), Zen.Or(Zen.Eq(y, x + 4), Zen.Eq(y, 3)))).Solve();
    Assert.False(model.IsSatisfiable());
  }

  [Fact]
  public static void TestAccessAstEnvironment()
  {
    const string envVar = "x";
    const string arg = "y";
    var env = new AstEnvironment(ImmutableDictionary<string, dynamic>.Empty.Add(envVar, 3));
    var f1 = new AstFunction<int>(arg, new Statement[]
    {
      new Assign(arg, new Plus(new Var(envVar), new Var(arg)))
    }).Evaluate(env);
    var y = Zen.Symbolic<int>();
    var model = Zen.Not(Zen.Eq(y + 3, f1(y))).Solve();
    Assert.False(model.IsSatisfiable());
  }
}

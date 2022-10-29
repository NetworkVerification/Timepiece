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
        new GetField(typeof(RouteEnvironment), typeof(BigInteger), rVar, pathLen),
        new BigIntExpr(BigInteger.One))
    );
    var r = Zen.Symbolic<RouteEnvironment>();
    var rIncremented = r.IncrementAsPathLength(BigInteger.One);
    var f = new AstFunction<RouteEnvironment>(route, new Statement[]
    {
      new IfThenElse(new Havoc(), new[] {new Assign(route, increment)}, new[] {new Assign(route, rVar)})
    });
    var zenF = new AstEnvironment().EvaluateFunction(f);
    // since the if is a havoc, we have that zenF(r) is either incremented or the same:
    var model = Zen.Not(Zen.And(Zen.Eq(zenF(r), r), Zen.Eq(zenF(r), rIncremented))).Solve();
    Assert.True(model.IsSatisfiable());
  }

  [Fact]
  public static void TestFunctionComposition()
  {
    const string arg = "x";
    // randomly choose to increment the LP by 1 or set it to 0
    var f1 = new AstFunction<RouteEnvironment>(arg, new Statement[]
    {
      new IfThenElse(new Havoc(),
        new[]
        {
          new Assign(arg,
            new WithField(new Var(arg), "Lp",
              new Plus(new GetField(typeof(RouteEnvironment), typeof(uint), new Var(arg), "Lp"), new UIntExpr(1))))
        },
        new[]
        {
          new Assign(arg, new WithField(new Var(arg), "Lp", new UIntExpr(0)))
        })
    });
    // return the argument with 3 added to its LP
    var f2 = new AstFunction<RouteEnvironment>(arg, new[]
    {
      new Assign(arg,
        new WithField(new Var(arg), "Lp",
          new Plus(new GetField(typeof(RouteEnvironment), typeof(uint), new Var(arg), "Lp"), new UIntExpr(3))))
    });
    f1.Rename(arg, "y");
    var f = new Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>(t =>
      new AstEnvironment().EvaluateFunction(f2)(new AstEnvironment().EvaluateFunction(f1)(t)));
    var x = Zen.Symbolic<RouteEnvironment>();
    var y = Zen.Symbolic<RouteEnvironment>();
    // check that there does not exist a model where f(x) == y and y != x + 4 and y != 3
    // note that we need to store the result of f(x) in y as separate calls to f(x) can choose
    // to branch differently, meaning f(x) == 3 and f(x) != 3 is satisfiable (pick the else branch, pick the then branch)
    var model = Zen.Not(Zen.Implies(Zen.Eq(f(x).GetLp(), y.GetLp()),
        Zen.Or(Zen.Eq(y.GetLp(), x.GetLp() + 4), Zen.Eq(y.GetLp(), 3))))
      .Solve();
    Assert.False(model.IsSatisfiable());
  }

  [Fact]
  public static void TestAccessAstEnvironment()
  {
    const string envVar = "x";
    const string arg = "y";
    var env = new AstEnvironment(ImmutableDictionary<string, dynamic>.Empty.Add(envVar, 3U),
      new Dictionary<string, AstFunction<RouteEnvironment>>(), null, false);
    var f1 = env.EvaluateFunction(new AstFunction<RouteEnvironment>(arg, new Statement[]
    {
      new Assign(arg,
        new WithField(new Var(arg), "Lp",
          new Plus(new GetField(typeof(RouteEnvironment), typeof(uint), new Var(arg), "Lp"), new Var(envVar))))
    }));
    var y = Zen.Symbolic<RouteEnvironment>();
    var model = Zen.Not(Zen.Eq(y.GetLp() + 3, f1(y).GetLp())).Solve();
    Assert.False(model.IsSatisfiable());
  }
}

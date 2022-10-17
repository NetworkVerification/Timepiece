using System.Numerics;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using Xunit;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public static class AstEnvironmentTests
{
  private static AstEnvironment<RouteEnvironment> _env = new();

  /// <summary>
  /// Helper method for checking that two values are always equal.
  /// </summary>
  /// <param name="expr1"></param>
  /// <param name="expr2"></param>
  /// <typeparam name="T"></typeparam>
  private static void AssertEqValid<T>(Zen<T> expr1, Zen<T> expr2)
  {
    var b = Zen.Not(Zen.Eq(expr1, expr2)).Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public static void EvaluateBoolExprs(bool e)
  {
    var evaluated = (Zen<bool>) _env.EvaluateExpr(new BoolExpr(e));
    var zen = Zen.Constant(e);
    AssertEqValid(evaluated, zen);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(1000000)]
  public static void EvaluateIntExprs(int e)
  {
    var evaluated = (Zen<int>) _env.EvaluateExpr(new IntExpr(e));
    var zen = Zen.Constant(e);
    AssertEqValid(evaluated, zen);
  }

  [Theory]
  [InlineData("foo")]
  [InlineData("bar")]
  public static void EvaluateStringExprs(string e)
  {
    var evaluated = (Zen<string>) _env.EvaluateExpr(new StringExpr(e));
    var zen = Zen.Constant(e);
    AssertEqValid(evaluated, zen);
  }

  [Fact]
  public static void EvaluateNoneExpr()
  {
    var zen = Option.Null<int>();
    var evaluated = (Zen<Option<int>>) _env.EvaluateExpr(new None(typeof(int)));
    AssertEqValid(evaluated, zen);
  }

  [Fact]
  public static void EvaluateDefaultRoute()
  {
    var zen = new RouteEnvironment();
    var evaluated = (Zen<RouteEnvironment>) _env.EvaluateExpr(AstEnvironment<RouteEnvironment>.DefaultRoute());
    AssertEqValid(evaluated, zen);
  }

  [Fact]
  public static void EvaluateAssignStmt()
  {
    const string name = "x";
    var env1 = _env.EvaluateStatement(new Assign(name, new IntExpr(0)));
    var evaluated = (Zen<int>) env1.EvaluateExpr(new Var(name));
    AssertEqValid(evaluated, Zen.Constant(0));
  }

  [Fact]
  public static void EvaluateVariableSwap()
  {
    const string var1 = "x";
    const string var2 = "y";
    const string tempVar = "z";
    var statements = new List<Statement>
    {
      new Assign(var1, new IntExpr(0)),
      new Assign(var2, new IntExpr(1)),
      new Assign(tempVar, new Var(var1)),
      new Assign(var1, new Var(var2)),
      new Assign(var2, new Var(tempVar))
    };
    var env1 = _env.EvaluateStatements(statements);
    var getVar1 = (Zen<int>) env1.EvaluateExpr(new Var(var1));
    var getVar2 = (Zen<int>) env1.EvaluateExpr(new Var(var2));
    AssertEqValid(getVar1, Zen.Constant(1));
    AssertEqValid(getVar2, Zen.Constant(0));
  }

  [Fact]
  public static void EvaluateIfStatementHavoc()
  {
    const string resultVar = "result";
    const int trueResult = 0;
    const int falseResult = 1;
    var statement = new IfThenElse(new Havoc(), new List<Statement>
    {
      new Assign(resultVar, new IntExpr(trueResult))
    }, new List<Statement>
    {
      new Assign(resultVar, new IntExpr(falseResult))
    });
    var env1 = _env.EvaluateStatement(statement);
    var result = (Zen<int>) env1[resultVar];
    var b = Zen.Not(Zen.Or(Zen.Eq(result, Zen.Constant(trueResult)), Zen.Eq(result, Zen.Constant(falseResult))))
      .Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Fact]
  public static void EvaluateGetField()
  {
    const string pathLen = "AsPathLength";
    var route = AstEnvironment<RouteEnvironment>.DefaultRoute();
    var statements = new Statement[]
    {
      new Assign("route", route),
      new Assign("route",
        new WithField(new Var("route"), "Value",
          new Equals(
            new BigIntExpr(0),
            new GetField(typeof(RouteEnvironment), typeof(BigInteger), new Var("route"), pathLen))))
    };
    var env1 = _env.EvaluateStatements(statements);
    var result = (Zen<RouteEnvironment>) env1["route"];
    var b = Zen.Not(result.GetValue()).Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Fact]
  public static void EvaluateIncrementFieldConstant()
  {
    const string pathLen = "AsPathLength";
    const string route = "route";
    var statements = new Statement[]
    {
      new Assign(route, AstEnvironment<RouteEnvironment>.DefaultRoute()),
      new Assign("len",
        new GetField(typeof(RouteEnvironment), typeof(BigInteger), new Var(route), pathLen)),
      new Assign(route,
        new WithField(new Var(route), pathLen,
          new Plus(new Var("len"), new BigIntExpr(BigInteger.One))))
    };
    var env1 = _env.EvaluateStatements(statements);
    var result = (Zen<RouteEnvironment>) env1[route];
    var incrementedRoute = Zen.Constant(new RouteEnvironment()).IncrementAsPathLength(BigInteger.One);
    AssertEqValid(result, incrementedRoute);
  }

  [Fact]
  public static void EvaluateIncrementFieldSymbolic()
  {
    const string pathLen = "AsPathLength";
    var route = Zen.Symbolic<RouteEnvironment>();
    const string rVar = "route";
    var env1 = _env.Update(rVar, route);
    const string lenVar = "len";
    var statements = new Statement[]
    {
      new Assign(lenVar,
        new GetField(typeof(RouteEnvironment), typeof(BigInteger), new Var(rVar), pathLen)),
      new Assign(rVar,
        new WithField(new Var(rVar), pathLen,
          new Plus(new Var(lenVar), new BigIntExpr(BigInteger.One))))
    };
    var env2 = env1.EvaluateStatements(statements);
    var result = (Zen<RouteEnvironment>) env2[rVar];
    var incrementedRoute = route.IncrementAsPathLength(BigInteger.One);
    AssertEqValid(result, incrementedRoute);
  }
}

using System.Numerics;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using Xunit;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public static class AstEnvironmentTests
{
  private static AstEnvironment _env = new();

  private static void AssertEqValid<T>(Zen<T> expr1, Zen<T> expr2)
  {
    var b = Zen.Not(Zen.Eq(expr1, expr2)).Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  [InlineData(0)]
  [InlineData("foo")]
  public static void EvaluateConstantExprs<T>(T e)
  {
    var evaluated = (Zen<T>) _env.EvaluateExpr(new ConstantExpr(e ?? throw new ArgumentNullException(nameof(e))));
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
  public static void EvaluateAssignStmt()
  {
    const string name = "x";
    var env1 = _env.EvaluateStatement(new Assign(name, new ConstantExpr(0)));
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
      new Assign(var1, new ConstantExpr(0)),
      new Assign(var2, new ConstantExpr(1)),
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
    const int trueResult = 0;
    const int falseResult = 1;
    var statement = new IfThenElse(new Havoc(), new List<Statement>
    {
      new Return(new ConstantExpr(trueResult))
    }, new List<Statement>
    {
      new Return(new ConstantExpr(falseResult))
    });
    var env1 = _env.EvaluateStatement(statement);
    var result = (Zen<int>) env1.Return();
    var b = Zen.Not(Zen.Or(Zen.Eq(result, Zen.Constant(trueResult)), Zen.Eq(result, Zen.Constant(falseResult))))
      .Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Fact]
  public static void EvaluateGetField()
  {
    const string pathLen = "AsPathLength";
    var statements = new Statement[]
    {
      new Assign("route", new ConstantExpr(new BatfishBgpRoute())),
      new Return(new GetField(typeof(BatfishBgpRoute), typeof(BigInteger), new Var("route"), pathLen))
    };
    var env1 = _env.EvaluateStatements(statements);
    var result = (Zen<BigInteger>) env1.Return();
    var b = Zen.Not(Zen.Eq(result, BigInteger.Zero)).Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Fact]
  public static void EvaluateIncrementFieldConstant()
  {
    const string pathLen = "AsPathLength";
    var statements = new Statement[]
    {
      new Assign("route", new ConstantExpr(new BatfishBgpRoute())),
      new Assign("len",
        new GetField(typeof(BatfishBgpRoute), typeof(BigInteger), new Var("route"), pathLen)),
      new Return(
        new WithField(new Var("route"), pathLen,
          new Plus(new Var("len"), new ConstantExpr(BigInteger.One))))
    };
    var env1 = _env.EvaluateStatements(statements);
    var result = (Zen<BatfishBgpRoute>) env1.Return();
    var incrementedRoute = Zen.Constant(new BatfishBgpRoute()).IncrementAsPathLength(BigInteger.One);
    var b = Zen.Not(Zen.Eq(result, incrementedRoute)).Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Fact]
  public static void EvaluateIncrementFieldSymbolic()
  {
    const string pathLen = "AsPathLength";
    var route = Zen.Symbolic<BatfishBgpRoute>();
    const string rVar = "route";
    var env1 = _env.Update(rVar, route);
    const string lenVar = "len";
    var statements = new Statement[]
    {
      new Assign(lenVar,
        new GetField(typeof(BatfishBgpRoute), typeof(BigInteger), new Var(rVar), pathLen)),
      new Return(
        new WithField(new Var(rVar), pathLen,
          new Plus(new Var(lenVar), new ConstantExpr(BigInteger.One))))
    };
    var env2 = env1.EvaluateStatements(statements);
    var result = (Zen<BatfishBgpRoute>) env2.Return();
    var incrementedRoute = route.IncrementAsPathLength(BigInteger.One);
    var b = Zen.Not(Zen.Eq(result, incrementedRoute)).Solve();
    Assert.False(b.IsSatisfiable());
  }
}

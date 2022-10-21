using System.Numerics;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Angler.UntypedAst.AstStmt;
using Xunit;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public static class AstEnvironmentTests
{
  private const string DefaultPolicy = "defaultPolicy";
  private const string WillFallThrough = "willFallThrough";
  private const string ExitReject = "exitReject";

  // AstFunction that exits and rejects the route.
  private static readonly AstFunction<RouteEnvironment> ExitRejectFunction = new("env", new Statement[]
  {
    new Assign("env",
      new WithField(new WithField(new Var("env"), "Exited", new BoolExpr(true)),
        "Value", new BoolExpr(false)))
  });

  // AstFunction that falls through
  private static readonly AstFunction<RouteEnvironment> FallThroughFunction = new("env", new Statement[]
  {
    new Assign("env",
      new WithField(new Var("env"), "FallThrough", new BoolExpr(true)))
  });

  // AstFunction that sets returned and value to true.
  private static readonly AstFunction<RouteEnvironment> ReturnTrueFunction = new("env", new Statement[]
  {
    new Assign("env",
      new WithField(new WithField(new Var("env"), "Returned", new BoolExpr(true)),
        "Value", new BoolExpr(true)))
  });

  // DefaultPolicy is a policy that just accepts a route.
  private static readonly AstEnvironment Env = new(new Dictionary<string, AstFunction<RouteEnvironment>>
  {
    {DefaultPolicy, ReturnTrueFunction},
    {WillFallThrough, FallThroughFunction},
    {ExitReject, ExitRejectFunction}
  });

  private static readonly Environment<RouteEnvironment> R = new(Zen.Symbolic<RouteEnvironment>());

  private static dynamic EvaluateExprIgnoreRoute(Expr e)
  {
    return Env.EvaluateExpr(R, e).returnValue;
  }

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
    var evaluated = (Zen<bool>) EvaluateExprIgnoreRoute(new BoolExpr(e));
    var zen = Zen.Constant(e);
    AssertEqValid(evaluated, zen);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(1000000)]
  public static void EvaluateIntExprs(int e)
  {
    var evaluated = (Zen<int>) EvaluateExprIgnoreRoute(new IntExpr(e));
    var zen = Zen.Constant(e);
    AssertEqValid(evaluated, zen);
  }

  [Theory]
  [InlineData("foo")]
  [InlineData("bar")]
  public static void EvaluateStringExprs(string e)
  {
    var evaluated = (Zen<string>) EvaluateExprIgnoreRoute(new StringExpr(e));
    var zen = Zen.Constant(e);
    AssertEqValid(evaluated, zen);
  }

  [Fact]
  public static void EvaluateNoneExpr()
  {
    var zen = Option.Null<int>();
    var evaluated = (Zen<Option<int>>) EvaluateExprIgnoreRoute(new None(typeof(int)));
    AssertEqValid(evaluated, zen);
  }

  [Fact]
  public static void EvaluateDefaultRoute()
  {
    var zen = new RouteEnvironment();
    var evaluated = (Zen<RouteEnvironment>) EvaluateExprIgnoreRoute(AstEnvironment.DefaultRoute());
    AssertEqValid(evaluated, zen);
  }

  [Fact]
  public static void EvaluateAssignStmt()
  {
    const string name = "x";
    var env1 = Env.EvaluateStatement(R, new Assign(name, new IntExpr(0)));
    var evaluated = (Zen<int>) env1.EvaluateExpr(R, new Var(name)).returnValue;
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
    var env1 = Env.EvaluateStatements(R, statements);
    var getVar1 = (Zen<int>) env1.EvaluateExpr(R, new Var(var1)).returnValue;
    var getVar2 = (Zen<int>) env1.EvaluateExpr(R, new Var(var2)).returnValue;
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
    var env1 = Env.EvaluateStatement(R, statement);
    var result = (Zen<int>) env1[resultVar];
    var b = Zen.Not(Zen.Or(Zen.Eq(result, Zen.Constant(trueResult)), Zen.Eq(result, Zen.Constant(falseResult))))
      .Solve();
    Assert.False(b.IsSatisfiable());
  }

  [Fact]
  public static void EvaluateGetField()
  {
    const string pathLen = "AsPathLength";
    var route = AstEnvironment.DefaultRoute();
    var statements = new Statement[]
    {
      new Assign("route", route),
      new Assign("route",
        new WithField(new Var("route"), "Value",
          new Equals(
            new BigIntExpr(0),
            new GetField(typeof(RouteEnvironment), typeof(BigInteger), new Var("route"), pathLen))))
    };
    var env1 = Env.EvaluateStatements(R, statements);
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
      new Assign(route, AstEnvironment.DefaultRoute()),
      new Assign(route, new WithField(new Var(route), pathLen,
        new Plus(new GetField(typeof(RouteEnvironment), typeof(BigInteger), new Var(route), pathLen),
          new BigIntExpr(BigInteger.One)))),
    };
    var env1 = Env.EvaluateStatements(R, statements);
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
    var env1 = Env.Update(rVar, route);
    const string lenVar = "len";
    var statements = new Statement[]
    {
      new Assign(lenVar,
        new GetField(typeof(RouteEnvironment), typeof(BigInteger), new Var(rVar), pathLen)),
      new Assign(rVar,
        new WithField(new Var(rVar), pathLen,
          new Plus(new Var(lenVar), new BigIntExpr(BigInteger.One))))
    };
    var env2 = env1.EvaluateStatements(R, statements);
    var result = (Zen<RouteEnvironment>) env2[rVar];
    var incrementedRoute = route.IncrementAsPathLength(BigInteger.One);
    AssertEqValid(result, incrementedRoute);
  }

  [Fact]
  public static void EvaluateReturnTrueFunction()
  {
    const string arg = "arg";
    var function = new AstFunction<RouteEnvironment>(arg, new Statement[]
    {
      new Assign(arg,
        new WithField(
          new WithField(new Var(arg), "Value", new BoolExpr(true)),
          "Returned", new BoolExpr(true))),
      new IfThenElse(new Or(new GetField(typeof(RouteEnvironment), typeof(bool), new Var(arg), "Exited"),
        new GetField(typeof(RouteEnvironment), typeof(bool), new Var(arg), "Returned")), new Statement[]
      {
        new Assign(arg, new WithField(new Var(arg), "Returned", new BoolExpr(false)))
      }, new Statement[]
      {
        new Assign(arg,
          new WithField(
            new WithField(new Var(arg), "Value",
              new GetField(typeof(RouteEnvironment), typeof(bool), new Var(arg), "LocalDefaultAction")),
            "FallThrough", new BoolExpr(true)))
      })
    });
    var evaluatedFunction = Env.EvaluateFunction(function);
    Zen<RouteEnvironment> ReturnTrue(Zen<RouteEnvironment> t) => t.WithValue(true).WithReturned(false);
    var inputRoute = Zen.Symbolic<RouteEnvironment>();
    AssertEqValid(ReturnTrue(inputRoute), evaluatedFunction(inputRoute));
  }

  [Theory]
  [InlineData(true, true, true)]
  [InlineData(true, false, false)]
  [InlineData(false, true, false)]
  [InlineData(false, false, false)]
  public static void EvaluateAndExprTruthTable(bool arg1, bool arg2, bool result)
  {
    var e = new And(new BoolExpr(arg1), new BoolExpr(arg2));
    var evaluated = EvaluateExprIgnoreRoute(e);
    AssertEqValid(evaluated, Zen.Constant(result));
  }

  [Theory]
  [InlineData(true, true, true)]
  [InlineData(true, false, true)]
  [InlineData(false, true, true)]
  [InlineData(false, false, false)]
  public static void EvaluateOrExprTruthTable(bool arg1, bool arg2, bool result)
  {
    var e = new Or(new BoolExpr(arg1), new BoolExpr(arg2));
    var evaluated = EvaluateExprIgnoreRoute(e);
    AssertEqValid(evaluated, Zen.Constant(result));
  }

  [Theory]
  [InlineData(true, false)]
  [InlineData(false, true)]
  public static void EvaluateNotExprTruthTable(bool arg, bool result)
  {
    var e = new Not(new BoolExpr(arg));
    var evaluated = EvaluateExprIgnoreRoute(e);
    AssertEqValid(evaluated, Zen.Constant(result));
  }

  [Fact]
  public static void EvaluateCallExpr()
  {
    var r = Zen.Symbolic<RouteEnvironment>();
    var e = new Call(DefaultPolicy);
    // evaluate the call on an AstEnvironment where DefaultPolicy is defined to just return true.
    var evaluated = Env.EvaluateExpr(new Environment<RouteEnvironment>(r), e);
    AssertEqValid(evaluated.returnValue, Zen.True());
    // returned will be reset to whatever it had been before the call
    AssertEqValid(evaluated.route, r.WithValue(true));
  }

  [Fact]
  public static void EvaluateFirstMatchChainDefaultPolicySet()
  {
    var r = Zen.Symbolic<RouteEnvironment>().WithExited(false);
    const string arg = "env";
    var statements = new Statement[]
    {
      new SetDefaultPolicy(DefaultPolicy),
      // set the value according to the result of FirstMatchChain
      new IfThenElse(new FirstMatchChain(), new Statement[]
        {
          new Assign(arg, new WithField(new Var(arg), "Value", new BoolExpr(true)))
        },
        new Statement[]
        {
          new Assign(arg, new WithField(new Var(arg), "Value", new BoolExpr(false)))
        }
      )
    };
    var evaluated = Env.Update(arg, r).EvaluateStatements(new Environment<RouteEnvironment>(r), statements);
    AssertEqValid(evaluated[arg], r.WithValue(true));
  }

  [Fact]
  public static void EvaluateFirstMatchChainFallThrough()
  {
    var r = Zen.Symbolic<RouteEnvironment>().WithExited(false);
    const string arg = "env";
    var statements = new Statement[]
    {
      new SetDefaultPolicy(DefaultPolicy),
      // set the value according to the result of FirstMatchChain
      new IfThenElse(new FirstMatchChain(new Call(WillFallThrough)), new Statement[]
        {
          new Assign(arg, new WithField(new Var(arg), "Value", new BoolExpr(true)))
        },
        new Statement[]
        {
          new Assign(arg, new WithField(new Var(arg), "Value", new BoolExpr(false)))
        }
      )
    };
    var evaluated = Env.Update(arg, r).EvaluateStatements(new Environment<RouteEnvironment>(r), statements);
    AssertEqValid(evaluated[arg], r.WithValue(true));
  }

  [Fact]
  public static void EvaluateFirstMatchChainNoDefaultPolicy()
  {
    var r = Zen.Symbolic<RouteEnvironment>().WithExited(false);
    const string arg = "env";
    var statements = new Statement[]
    {
      new IfThenElse(new FirstMatchChain(), new Statement[]
        {
          new Assign(arg, new WithField(new Var(arg), "Value", new BoolExpr(true)))
        },
        new Statement[]
        {
          new Assign(arg, new WithField(new Var(arg), "Value", new BoolExpr(false)))
        }
      )
    };
    Assert.Throws<Exception>(() =>
      Env.Update(arg, r).EvaluateStatements(new Environment<RouteEnvironment>(r), statements));
  }
}

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Timepiece.Angler.Ast.AstExpr;
using Timepiece.Angler.Ast.AstStmt;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
///   An immutable state of the AST environment.
/// </summary>
public record AstEnvironment(ImmutableDictionary<string, dynamic> Bindings,
  IReadOnlyDictionary<string, AstFunction<RouteEnvironment>> Declarations,
  string? DefaultPolicy = null, bool CallExprContext = false, bool TrackTerms = false)
{
  /// <summary>
  ///   The Zen null option creation method.
  /// </summary>
  private static readonly MethodInfo Null =
    typeof(Option).GetMethod("Null") ?? throw new Exception("Option.Null method not found");

  /// <summary>
  ///   The Zen record creation method.
  /// </summary>
  private static readonly MethodInfo Create =
    typeof(Zen).GetMethod("Create") ?? throw new Exception("Zen.Create method not found");

  private static readonly MethodInfo GetField =
    typeof(Zen).GetMethod("GetField") ?? throw new Exception("Zen.GetField method not found");

  /// <summary>
  /// Construct a new AstEnvironment with no bound variables.
  /// </summary>
  /// <param name="declarations"></param>
  /// <param name="defaultPolicy"></param>
  /// <param name="callExprContext"></param>
  /// <param name="trackTerms"></param>
  public AstEnvironment(IReadOnlyDictionary<string, AstFunction<RouteEnvironment>> declarations,
    string? defaultPolicy = null, bool callExprContext = false, bool trackTerms = false) : this(
    ImmutableDictionary<string, dynamic>.Empty, declarations, defaultPolicy, callExprContext, trackTerms)
  {
  }

  public AstEnvironment() : this(new Dictionary<string, AstFunction<RouteEnvironment>>())
  {
  }

  public dynamic this[string var] => Bindings[var];

  /// <summary>
  ///   Update the environment with the given value at the given variable,
  ///   possibly overwriting a previous value.
  /// </summary>
  /// <param name="var"></param>
  /// <param name="val"></param>
  /// <returns></returns>
  public AstEnvironment Update(string var, dynamic val)
  {
    return this with {Bindings = Bindings.SetItem(var, val)};
  }

  // FIXME: should this take an additional argument representing the current state of the input variable?
  // could we then modify this state when returning from a Call?
  public ReturnEnvironment<RouteEnvironment> EvaluateExpr(ReturnEnvironment<RouteEnvironment> env, Expr e)
  {
    if (e is null) throw new ArgumentNullException(nameof(e), "Given a null expression.");

    // We assume that only certain expressions consider possible modifications to the route
    // after being called: these are Call expressions and the boolean operations And, Or and Not.
    // For all other expressions, we ignore the possibility that an inner EvaluateExpr call might
    // modify the route when it returns and use ignoreRoute.
    var ignoreRoute = new Func<Expr, dynamic>(e1 =>
    {
      var updatedEnv = EvaluateExpr(env, e1);
      // the updated environment must be the same as the one passed in
      Debug.Assert(updatedEnv.EqualRoutes(env),
        (string?) $"Unexpected route modification during evaluation of expr {e1}!");
      return updatedEnv.ReturnValue;
    });

    switch (e)
    {
      case Call c:
        var oldReturn = env.Route.GetResultReturned();
        // call the function with the current route as its argument
        var outputRoute = (this with {CallExprContext = true}).EvaluateFunction(Declarations[c.Name])(
          env.Route.WithResultReturned(false));
        // return the updated result and its associated value
        return new ReturnEnvironment<RouteEnvironment>(outputRoute.WithResultReturned(oldReturn),
          outputRoute.GetResultValue());
      case AstExpr.CallExprContext:
        return env with {ReturnValue = Zen.Constant(CallExprContext)};
      case ConjunctionChain cc:
        return cc.Evaluate(this, env);
      case FirstMatchChain fmc:
        return fmc.Evaluate(this, env);
      case ConstantExpr c:
        return env with {ReturnValue = c.constructor(c.value)};
      case LiteralSet s:
        return env with
        {
          ReturnValue = s.elements.Aggregate(CSet.Empty<string>(),
            (set, element) => CSet.Add<string>(set, ignoreRoute(element)))
        };
      case CreateRecord r:
        return env with
        {
          ReturnValue = Create.MakeGenericMethod(r.RecordType)
            .Invoke(null, new object?[] {r.GetFields(ignoreRoute)})!
        };
      // GetField could be implemented using UnaryOpExpr, but we write it like this just so that
      // the expensive reflection code isn't called at every call to UnaryOpExpr.unaryOp.
      case GetField g:
        return env with
        {
          ReturnValue = GetField.MakeGenericMethod(g.RecordType, g.FieldType)
            .Invoke(null, new object?[] {ignoreRoute(g.Record), g.FieldName})!
        };
      case Var v:
        return env with {ReturnValue = this[v.Name]};
      case Havoc:
        return env with {ReturnValue = Zen.Symbolic<bool>()};
      case None n:
        return env with {ReturnValue = Null.MakeGenericMethod(n.innerType).Invoke(null, null)!};
      case Not ne:
        // Since Not can wrap around boolean expressions that modify the route, such as Call, it is handled separately
        // from other UnaryOpExprs.
        var notEnv = EvaluateExpr(env, ne.expr);
        return notEnv with {ReturnValue = ne.unaryOp(notEnv.ReturnValue)};
      case UnaryOpExpr uoe:
        return env with {ReturnValue = uoe.unaryOp(ignoreRoute(uoe.expr))};
      case And ae:
        // Short-circuiting behavior of And and Or means that the Route might be different depending on which components execute.
        // evaluate the first conjunct
        // if its return value is false, the final env will be the first conjunct's
        var firstConjunctEnv = EvaluateExpr(env, ae.expr1);
        // otherwise, the final env will be the second conjunct's
        var secondConjunctEnv = EvaluateExpr(firstConjunctEnv, ae.expr2);
        return new ReturnEnvironment<RouteEnvironment>(
          Zen.If(firstConjunctEnv.ReturnValue, secondConjunctEnv.Route, firstConjunctEnv.Route),
          ae.binaryOp(firstConjunctEnv.ReturnValue, secondConjunctEnv.ReturnValue));
      case Or oe:
        // Short-circuiting behavior of And and Or means that the Route might be different depending on which components execute.
        // evaluate the first disjunct
        // if its return value is true, the final env will be the first disjunct's
        var firstDisjunctEnv = EvaluateExpr(env, oe.expr1);
        // otherwise, the final env will be the second disjunct's
        var secondDisjunctEnv = EvaluateExpr(firstDisjunctEnv, oe.expr2);
        return new ReturnEnvironment<RouteEnvironment>(
          Zen.If(firstDisjunctEnv.ReturnValue, firstDisjunctEnv.Route, secondDisjunctEnv.Route),
          oe.binaryOp(firstDisjunctEnv.ReturnValue, secondDisjunctEnv.ReturnValue));
      case BinaryOpExpr boe:
        return env with {ReturnValue = boe.binaryOp(ignoreRoute(boe.expr1), ignoreRoute(boe.expr2))};
      default:
        throw new ArgumentOutOfRangeException(nameof(e), $"{e} is not an expr I know how to handle!");
    }
  }

  public AstEnvironment EvaluateStatement(string arg, ReturnEnvironment<RouteEnvironment> route, Statement s)
  {
    switch (s)
    {
      case SetDefaultPolicy setDefaultPolicy:
        return this with {DefaultPolicy = setDefaultPolicy.PolicyName};
      case Assign a:
        var env = EvaluateExpr(route, a.Expr);
        // we assume that we never separately update the result in an assignment
        // (i.e. we never use Call within an Assign statement)
        Debug.Assert(a.Expr.GetType() != typeof(Call));
        return Update(a.Var, env.ReturnValue);
      case IfThenElse ite:
        // add the comment as a term (if track terms is on) to keep track of which policy terms were visited
        var withTerm = ite.Comment is null || !TrackTerms
          ? route
          : route with {Route = route.Route.AddVisitedTerm(ite.Comment)};
        var guardEnv = EvaluateExpr(withTerm, ite.Guard);
        // if the guard updated the route (e.g. by evaluating a Call),
        // we need to make sure those updates are observed in the branches // by using Update() here
        var newEnv = Update(arg, guardEnv.Route);
        return newEnv.EvaluateStatements(arg, guardEnv, ite.ThenCase)
          .Join(newEnv.EvaluateStatements(arg, guardEnv, ite.ElseCase), guardEnv.ReturnValue);
      default:
        throw new ArgumentOutOfRangeException(nameof(s));
    }
  }

  /// <summary>
  ///   Evaluate the given sequence of statements.
  ///   After each statement executes, we run the new statement with the new route according to the value assigned to arg.
  /// </summary>
  /// <param name="arg"></param>
  /// <param name="route"></param>
  /// <param name="statements"></param>
  /// <returns></returns>
  public AstEnvironment EvaluateStatements(string arg, ReturnEnvironment<RouteEnvironment> route,
    IEnumerable<Statement> statements)
  {
    return statements.Aggregate(this,
      (env, s) => env.EvaluateStatement(arg, route with {Route = (Zen<RouteEnvironment>) env[arg]}, s));
  }

  public Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> EvaluateFunction(AstFunction<RouteEnvironment> function)
  {
    return t =>
      Update(function.Arg, t)
        .EvaluateStatements(function.Arg, new ReturnEnvironment<RouteEnvironment>(t), function.Body)[
          function.Arg];
  }

  private AstEnvironment Join(AstEnvironment other, Zen<bool> guard)
  {
    if (other.Bindings.Any(p => !Bindings.ContainsKey(p.Key)))
      throw new ArgumentException("Environments do not bind the same variables.");

    var e = new AstEnvironment(Declarations, DefaultPolicy, CallExprContext);
    foreach (var (variable, value) in Bindings)
    {
      if (!other.Bindings.ContainsKey(variable))
        throw new ArgumentException($"{variable} not bound in both environments", nameof(other));

      var updatedValue = Zen.If(guard, value, other[variable]);
      e = e.Update(variable, updatedValue);
    }

    return e;
  }

  /// <summary>
  ///   Return a CreateRecord expression that evaluates to the default RouteEnvironment.
  /// </summary>
  /// <returns>A CreateRecord AST expr.</returns>
  public static CreateRecord DefaultRoute()
  {
    var env = new RouteEnvironment();
    return new CreateRecord("TEnvironment", new Dictionary<string, Expr>
    {
      {"Prefix", new PrefixExpr(env.Prefix)},
      {"Weight", new UIntExpr(env.Weight)},
      {"Lp", new UIntExpr(env.Lp)},
      {"AsSet", LiteralSet.Empty()},
      {"AsPathLength", new BigIntExpr(env.AsPathLength)},
      {"Metric", new UIntExpr(env.Metric)},
      {"Tag", new UIntExpr(env.Tag)},
      {"OriginType", new UInt2Expr(env.OriginType)},
      {"Communities", LiteralSet.Empty()},
      {"Result", ResultToRecord(new RouteResult())},
      {"LocalDefaultAction", new BoolExpr(env.LocalDefaultAction)},
      {"VisitedTerms", LiteralSet.Empty()}
    });
  }

  public static CreateRecord ResultToRecord(RouteResult result)
  {
    return new CreateRecord("TResult", new Dictionary<string, Expr>
    {
      {"Value", new BoolExpr(result.Value)},
      {"Exit", new BoolExpr(result.Exit)},
      {"Fallthrough", new BoolExpr(result.Fallthrough)},
      {"Returned", new BoolExpr(result.Returned)}
    });
  }
}

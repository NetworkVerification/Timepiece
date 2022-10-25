using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Angler.UntypedAst.AstStmt;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public class AstEnvironment
{
  private readonly ImmutableDictionary<string, dynamic> _env;
  private readonly IReadOnlyDictionary<string, AstFunction<RouteEnvironment>> _declarations;
  public readonly string? defaultPolicy;

  public AstEnvironment(ImmutableDictionary<string, dynamic> env,
    IReadOnlyDictionary<string, AstFunction<RouteEnvironment>> declarations,
    string? defaultPolicy)
  {
    _env = env;
    _declarations = declarations;
    this.defaultPolicy = defaultPolicy;
  }

  public AstEnvironment(ImmutableDictionary<string, dynamic> env,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations) : this(env, declarations, null)
  {
  }

  public dynamic this[string var] => _env[var];

  public AstEnvironment(IReadOnlyDictionary<string, AstFunction<RouteEnvironment>> declarations) : this(
    ImmutableDictionary<string, dynamic>.Empty, declarations, null)
  {
  }

  public AstEnvironment() : this(new Dictionary<string, AstFunction<RouteEnvironment>>())
  {
  }

  /// <summary>
  /// Update the environment with the given value at the given variable,
  /// possibly overwriting a previous value.
  /// </summary>
  /// <param name="var"></param>
  /// <param name="val"></param>
  /// <returns></returns>
  public AstEnvironment Update(string var, dynamic val)
  {
    return new AstEnvironment(_env.SetItem(var, val), _declarations, defaultPolicy);
  }

  public AstEnvironment WithDefaultPolicy(string policy)
  {
    return new AstEnvironment(_env, _declarations, policy);
  }

  // FIXME: should this take an additional argument representing the current state of the input variable?
  // could we then modify this state when returning from a Call?
  public Environment<RouteEnvironment> EvaluateExpr(Environment<RouteEnvironment> env, Expr e)
  {
    if (e is null)
    {
      throw new ArgumentNullException(nameof(e), "Given a null expression.");
    }

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
      return updatedEnv.returnValue;
    });

    switch (e)
    {
      case Call c:
        var oldReturn = env.route.GetResult().GetReturned();
        // call the function with the current route as its argument
        var callEnv =
          EvaluateFunction(_declarations[c.Name])(env.route.WithResult(env.route.GetResult().WithReturned(false)));
        // return the updated result and its associated value
        return new Environment<RouteEnvironment>(callEnv.WithResult(callEnv.GetResult().WithReturned(oldReturn)),
          callEnv.GetResult().GetValue());
      case ConjunctionChain cc:
        return cc.Evaluate(this, env);
      case FirstMatchChain fmc:
        return fmc.Evaluate(this, env);
      case ConstantExpr c:
        return env.WithValue(c.constructor(c.value));
      case LiteralSet s:
        return env.WithValue(s.elements.Aggregate(CSet.Empty<string>(),
          (set, element) => CSet.Add<string>(set, ignoreRoute(element))));
      case CreateRecord r:
        return env.WithValue(
          Create.MakeGenericMethod(r.RecordType)
            .Invoke(null, new object?[] {r.GetFields(ignoreRoute)})!);
      // GetField could be implemented using UnaryOpExpr, but we write it like this just so that
      // the expensive reflection code isn't called at every call to UnaryOpExpr.unaryOp.
      case GetField g:
        return env.WithValue(GetField.MakeGenericMethod(g.RecordType, g.FieldType)
          .Invoke(null, new object?[] {ignoreRoute(g.Record), g.FieldName})!);
      case Var v:
        return env.WithValue(this[v.Name]);
      case Havoc:
        return env.WithValue(Zen.Symbolic<bool>());
      case None n:
        return env.WithValue(Null.MakeGenericMethod(n.innerType).Invoke(null, null)!);
      case Not ne:
        var notEnv = EvaluateExpr(env, ne.expr);
        return notEnv.WithValue(ne.unaryOp(notEnv.returnValue));
      case UnaryOpExpr uoe:
        return env.WithValue(uoe.unaryOp(ignoreRoute(uoe.expr)));
      case PrefixContains:
        return env.WithValue(Zen.Symbolic<bool>()); // TODO: for now, we skip trying to handle prefixes
      case And ae:
        // evaluate the first conjunct
        // if its return value is false, the final env will be the first conjunct's
        var firstConjunctEnv = EvaluateExpr(env, ae.expr1);
        // otherwise, the final env will be the second conjunct's
        var secondConjunctEnv = EvaluateExpr(firstConjunctEnv, ae.expr2);
        return firstConjunctEnv.WithValue(ae.binaryOp(firstConjunctEnv.returnValue, secondConjunctEnv.returnValue))
          .WithRoute(Zen.If(firstConjunctEnv.returnValue, secondConjunctEnv.route, firstConjunctEnv.route));
      case Or oe:
        // evaluate the first disjunct
        // if its return value is true, the final env will be the first disjunct's
        var firstDisjunctEnv = EvaluateExpr(env, oe.expr1);
        // otherwise, the final env will be the second disjunct's
        var secondDisjunctEnv = EvaluateExpr(firstDisjunctEnv, oe.expr2);
        return firstDisjunctEnv.WithValue(oe.binaryOp(firstDisjunctEnv.returnValue, secondDisjunctEnv.returnValue))
          .WithRoute(Zen.If(firstDisjunctEnv.returnValue, firstDisjunctEnv.route, secondDisjunctEnv.route));
      case BinaryOpExpr boe:
        return env.WithValue(boe.binaryOp(ignoreRoute(boe.expr1), ignoreRoute(boe.expr2)));
      default:
        throw new ArgumentOutOfRangeException(nameof(e), $"{e} is not an expr I know how to handle!");
    }
  }

  public AstEnvironment EvaluateStatement(Environment<RouteEnvironment> route, Statement s)
  {
    switch (s)
    {
      case SetDefaultPolicy setDefaultPolicy:
        return WithDefaultPolicy(setDefaultPolicy.Name);
      case Assign a:
        var env = EvaluateExpr(route, a.Expr);
        // TODO: should we do something with the route result?
        // we assume that we never update the result in an assignment
        return Update(a.Var, env.returnValue);
      case IfThenElse ite:
        var guardEnv = EvaluateExpr(route, ite.Guard);
        return EvaluateStatements(guardEnv, ite.ThenCase)
          .Join(EvaluateStatements(guardEnv, ite.ElseCase), guardEnv.returnValue);
      default:
        throw new ArgumentOutOfRangeException(nameof(s));
    }
  }

  public AstEnvironment EvaluateStatements(Environment<RouteEnvironment> route, IEnumerable<Statement> statements) =>
    statements.Aggregate(this, (env, s) => env.EvaluateStatement(route, s));

  public Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> EvaluateFunction(AstFunction<RouteEnvironment> function)
  {
    return t =>
      Update(function.Arg, t).EvaluateStatements(new Environment<RouteEnvironment>(t), function.Body)[
        function.Arg];
  }

  private AstEnvironment Join(AstEnvironment other, Zen<bool> guard)
  {
    if (other._env.Any(p => !_env.ContainsKey(p.Key)))
    {
      throw new ArgumentException("Environments do not bind the same variables.");
    }

    var e = new AstEnvironment(_declarations);
    foreach (var (variable, value) in _env)
    {
      if (!other._env.ContainsKey(variable))
      {
        throw new ArgumentException($"{variable} not bound in both environments", nameof(other));
      }

      var updatedValue = Zen.If(guard, value, other[variable]);
      e = e.Update(variable, updatedValue);
    }

    return e;
  }

  /// <summary>
  /// Return a CreateRecord expression that evaluates to the default RouteEnvironment.
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
      {"AsPathLength", new BigIntExpr(env.AsPathLength)},
      {"Metric", new UIntExpr(env.Metric)},
      {"Tag", new UIntExpr(env.Tag)},
      {"OriginType", new UInt2Expr(env.OriginType)},
      {"Communities", LiteralSet.Empty()},
      {"Result", ResultToRecord(new RouteResult())},
      {"LocalDefaultAction", new BoolExpr(env.LocalDefaultAction)},
    });
  }

  public static CreateRecord ResultToRecord(RouteResult result)
  {
    return new CreateRecord("TResult", new Dictionary<string, Expr>
    {
      {"Value", new BoolExpr(result.Value)},
      {"Exit", new BoolExpr(result.Exit)},
      {"Fallthrough", new BoolExpr(result.Fallthrough)},
      {"Returned", new BoolExpr(result.Returned)},
    });
  }

  /// <summary>
  /// The Zen null option creation method.
  /// </summary>
  internal static readonly MethodInfo Null =
    typeof(Option).GetMethod("Null") ?? throw new Exception("Option.Null method not found");

  /// <summary>
  /// The Zen record creation method.
  /// </summary>
  internal static readonly MethodInfo Create =
    typeof(Zen).GetMethod("Create") ?? throw new Exception("Zen.Create method not found");

  internal static readonly MethodInfo GetField =
    typeof(Zen).GetMethod("GetField") ?? throw new Exception("Zen.GetField method not found");
}

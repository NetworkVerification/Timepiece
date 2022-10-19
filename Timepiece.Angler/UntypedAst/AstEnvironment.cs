using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Angler.UntypedAst.AstStmt;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public partial class AstEnvironment
{
  private readonly ImmutableDictionary<string, dynamic> _env;
  private readonly Dictionary<string, AstFunction<RouteEnvironment>> _declarations;


  public AstEnvironment(ImmutableDictionary<string, dynamic> env,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations)
  {
    _env = env;
    _declarations = declarations;
  }

  public dynamic this[string var] => _env[var];

  public AstEnvironment(Dictionary<string, AstFunction<RouteEnvironment>> declarations) : this(
    ImmutableDictionary<string, dynamic>.Empty, declarations)
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
    return new AstEnvironment(_env.SetItem(var, val), _declarations);
  }

  // FIXME: should this take an additional argument representing the current state of the input variable?
  // could we then modify this state when returning from a Call?
  public (Zen<RouteEnvironment>, dynamic) EvaluateExpr(Zen<RouteEnvironment> route, Expr e)
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
      var (updatedRoute, result) = EvaluateExpr(route, e1);
      // the updated route must be the same as the one passed in
      Debug.Assert(!Zen.Not(Zen.Eq(route, updatedRoute)).Solve().IsSatisfiable());
      return result;
    });

    switch (e)
    {
      case Call c:
        // FIXME: Call has side effects: the evaluated function updates the variable in the calling context when it returns
        var result = EvaluateFunction(_declarations[c.Name])(route);
        // return the updated result and its associated value
        return (result, result.GetValue());
      case ConstantExpr c:
        return (route, c.constructor(c.value));
      case LiteralSet s:
        return (route, s.elements.Aggregate(CSet.Empty<string>(),
          (set, element) => CSet.Add<string>(set, ignoreRoute(element))));
      case CreateRecord r:
        return (route,
          AstEnvironment.Create.MakeGenericMethod(r.RecordType)
            .Invoke(null, new object?[] {r.GetFields(ignoreRoute)})!);
      // GetField could be implemented using UnaryOpExpr, but we write it like this just so that
      // the expensive reflection code isn't called at every call to UnaryOpExpr.unaryOp.
      case GetField g:
        return (route, AstEnvironment.GetField.MakeGenericMethod(g.RecordType, g.FieldType)
          .Invoke(null, new object?[] {ignoreRoute(g.Record), g.FieldName})!);
      case Var v:
        return (route, this[v.Name]);
      case Havoc:
        return (route, Zen.Symbolic<bool>());
      case None n:
        return (route, AstEnvironment.Null.MakeGenericMethod(n.innerType).Invoke(null, null)!);
      case Not ne:
        var (routeResult, innerResult) = EvaluateExpr(route, ne);
        return (routeResult, ne.unaryOp(innerResult));
      case UnaryOpExpr uoe:
        return (route, uoe.unaryOp(ignoreRoute(uoe.expr)));
      case PrefixContains:
        return (route, Zen.Symbolic<bool>()); // TODO: for now, we skip trying to handle prefixes
      case And ae:
        var (firstConjunctRoute, firstConjunctResult) = EvaluateExpr(route, ae.expr1);
        var (secondConjunctRoute, secondConjunctResult) = EvaluateExpr(
          Zen.If(firstConjunctRoute.GetValue(), firstConjunctRoute, route), ae.expr2);
        return (secondConjunctRoute, ae.binaryOp(firstConjunctResult, secondConjunctResult));
      case Or oe:
        var (firstDisjunctRoute, firstDisjunctResult) = EvaluateExpr(route, oe.expr1);
        var (secondDisjunctRoute, secondDisjunctResult) = EvaluateExpr(
          Zen.If(firstDisjunctRoute.GetValue(), route, firstDisjunctRoute), oe.expr2);
        return (secondDisjunctRoute, oe.binaryOp(firstDisjunctResult, secondDisjunctResult));
      case BinaryOpExpr boe:
        return (route, boe.binaryOp(ignoreRoute(boe.expr1), ignoreRoute(boe.expr2)));
      default:
        throw new ArgumentOutOfRangeException(nameof(e), $"{e} is not an expr I know how to handle!");
    }
  }

  public AstEnvironment EvaluateStatement(Zen<RouteEnvironment> route, Statement s)
  {
    switch (s)
    {
      case Assign a:
        var (_, result) = EvaluateExpr(route, a.Expr);
        // TODO: should we do something with the route result?
        // we assume that we never update the result in an assignment
        return Update(a.Var, result);
      case IfThenElse ite:
        var (routeResult, guard) = EvaluateExpr(route, ite.Guard);
        return EvaluateStatements(routeResult, ite.ThenCase)
          .Join(EvaluateStatements(routeResult, ite.ElseCase), guard);
      default:
        throw new ArgumentOutOfRangeException(nameof(s));
    }
  }

  public AstEnvironment EvaluateStatements(Zen<RouteEnvironment> route, IEnumerable<Statement> statements) =>
    statements.Aggregate(this, (env, s) => env.EvaluateStatement(route, s));

  public Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> EvaluateFunction(AstFunction<RouteEnvironment> function)
  {
    return t => Update(function.Arg, t).EvaluateStatements(t, function.Body)[function.Arg];
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
      {"Value", new BoolExpr(env.Value)},
      {"Exited", new BoolExpr(env.Exited)},
      {"FallThrough", new BoolExpr(env.FallThrough)},
      {"Returned", new BoolExpr(env.Returned)},
      {"LocalDefaultAction", new BoolExpr(env.LocalDefaultAction)},
    });
  }
}

public partial class AstEnvironment
{
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

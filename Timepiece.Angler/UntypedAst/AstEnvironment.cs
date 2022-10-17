using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Angler.UntypedAst.AstStmt;
using Timepiece.Datatypes;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public class AstEnvironment<T>
{
  private readonly ImmutableDictionary<string, dynamic> _env;
  private readonly Dictionary<string, AstFunction<T>> _declarations;

  /// <summary>
  /// The Zen null option creation method.
  /// </summary>
  private static readonly MethodInfo NullMethod =
    typeof(Option).GetMethod("Null") ?? throw new Exception("Option.Null method not found");

  /// <summary>
  /// The Zen record creation method.
  /// </summary>
  private static readonly MethodInfo CreateMethod =
    typeof(Zen).GetMethod("Create") ?? throw new Exception("Zen.Create method not found");

  private static readonly MethodInfo GetFieldMethod =
    typeof(Zen).GetMethod("GetField") ?? throw new Exception("Zen.GetField method not found");

  public AstEnvironment(ImmutableDictionary<string, dynamic> env, Dictionary<string, AstFunction<T>> declarations)
  {
    _env = env;
    _declarations = declarations;
  }

  public dynamic this[string var] => _env[var];

  public AstEnvironment(Dictionary<string, AstFunction<T>> declarations) : this(
    ImmutableDictionary<string, dynamic>.Empty, declarations)
  {
  }

  public AstEnvironment() : this(new Dictionary<string, AstFunction<T>>())
  {
  }

  /// <summary>
  /// Update the environment with the given value at the given variable,
  /// possibly overwriting a previous value.
  /// </summary>
  /// <param name="var"></param>
  /// <param name="val"></param>
  /// <returns></returns>
  public AstEnvironment<T> Update(string var, dynamic val)
  {
    return new AstEnvironment<T>(_env.SetItem(var, val), _declarations);
  }

  public dynamic EvaluateExpr(Expr e)
  {
    if (e is null)
    {
      throw new ArgumentNullException(nameof(e), "Given a null expression.");
    }

    return e switch
    {
      Call c => EvaluateStatements(_declarations[c.Name].Body),
      BoolExpr b => Zen.Constant<bool>(b.value),
      IntExpr i => Zen.Constant<int>(i.value),
      UInt2Expr i => Zen.Constant<UInt<_2>>(i.value),
      UIntExpr u => Zen.Constant<uint>(u.value),
      BigIntExpr b => Zen.Constant<BigInteger>(b.value),
      PrefixExpr p => Zen.Constant<Ipv4Prefix>(p.value),
      // strings used with CSets should be literal C# values, not Zen<string>
      StringExpr s => s.value,
      LiteralSet s => s.elements.Aggregate(CSet.Empty<string>(),
        (set, element) => CSet.Add<string>(set, EvaluateExpr(element))),
      CreateRecord r => CreateMethod.MakeGenericMethod(r.RecordType)
        .Invoke(null, new object?[] {r.GetFields(EvaluateExpr)})!,
      // GetField could be implemented using UnaryOpExpr, but we write it like this just so that
      // the expensive reflection code isn't called at every call to UnaryOpExpr.unaryOp.
      GetField g => GetFieldMethod.MakeGenericMethod(g.RecordType, g.FieldType)
        .Invoke(null, new object?[] {EvaluateExpr(g.Record), g.FieldName})!,
      Var v => this[v.Name],
      Havoc => Zen.Symbolic<bool>(),
      None n => NullMethod.MakeGenericMethod(n.innerType).Invoke(null, null)!,
      UnaryOpExpr uoe => uoe.unaryOp(EvaluateExpr(uoe.expr)),
      PrefixContains => Zen.Symbolic<bool>(), // TODO: for now, we skip trying to handle prefixes
      BinaryOpExpr boe => boe.binaryOp(EvaluateExpr(boe.expr1), EvaluateExpr(boe.expr2)),
      _ => throw new ArgumentOutOfRangeException(nameof(e), $"{e} is not an expr I know how to handle!"),
    };
  }

  public AstEnvironment<T> EvaluateStatement(Statement s)
  {
    return s switch
    {
      Assign a => Update(a.Var, EvaluateExpr(a.Expr)),
      IfThenElse ite => EvaluateStatements(ite.ThenCase)
        .Join(EvaluateStatements(ite.ElseCase), EvaluateExpr(ite.Guard)),
      _ => throw new ArgumentOutOfRangeException(nameof(s))
    };
  }

  public AstEnvironment<T> EvaluateStatements(IEnumerable<Statement> statements) =>
    statements.Aggregate(this, (env, s) => env.EvaluateStatement(s));

  private AstEnvironment<T> Join(AstEnvironment<T> other, Zen<bool> guard)
  {
    // FIXME: we need a way to deal with the case that one environment returns and the other does not;
    // when this happens, ReturnValue will be bound in one environment but not the other, making it unclear
    // what its value should be
    var e = new AstEnvironment<T>(_declarations);
    foreach (var (variable, value) in _env)
    {
      if (!other._env.ContainsKey(variable))
      {
        throw new ArgumentException($"{variable} not bound in both environments", nameof(other));
      }

      var updatedValue = Zen.If(guard, value, other[variable]);
      e = e.Update(variable, updatedValue);
    }

    // add any variables that were not present in this but are in other
    if (other._env.Any(p => !_env.ContainsKey(p.Key)))
    {
      // e = e.Update(variable, Zen.If(guard, null, value));
      throw new ArgumentException("Environments do not bind the same variables.");
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

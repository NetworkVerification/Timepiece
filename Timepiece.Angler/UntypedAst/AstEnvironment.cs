using System.Collections.Immutable;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public class AstEnvironment
{
  private readonly ImmutableDictionary<string, dynamic> _env;
  private const string ReturnValue = "##return##";

  public AstEnvironment(ImmutableDictionary<string, dynamic> env)
  {
    _env = env;
  }

  private dynamic this[string var] => _env[var];

  public AstEnvironment() : this(ImmutableDictionary<string, dynamic>.Empty)
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
    return new AstEnvironment(_env.SetItem(var, val));
  }

  public dynamic Return() => this[ReturnValue];

  public dynamic EvaluateExpr(Expr e)
  {
    return e switch
    {
      Call => throw new NotImplementedException(),
      ConstantExpr constant => Zen.Constant(constant.value),
      EmptySet => Set.Empty<string>(),
      GetField getField => getField.Op(EvaluateExpr(getField.record), getField.fieldName),
      Var v => this[v.Name],
      WithField withField =>
        withField.Op(EvaluateExpr(withField.record), withField.fieldName, EvaluateExpr(withField.fieldValue)),
      Havoc => Zen.Symbolic<bool>(),
      None n => typeof(Option).GetMethod("Null")!.MakeGenericMethod(n.innerType).Invoke(null, null)!,
      UnaryOpExpr uoe => uoe.unaryOp(EvaluateExpr(uoe.expr)),
      BinaryOpExpr boe => boe.binaryOp(EvaluateExpr(boe.expr1), EvaluateExpr(boe.expr2)),
      _ => throw new ArgumentOutOfRangeException(nameof(e)),
    };
  }

  public AstEnvironment EvaluateStatement(Statement s)
  {
    return s switch
    {
      Assign a => Update(a.Name, EvaluateExpr(a.Expr)),
      IfThenElse ite => EvaluateStatements(ite.ThenCase)
        .Join(EvaluateStatements(ite.ElseCase), EvaluateExpr(ite.Guard)),
      Return rt => Update(ReturnValue, EvaluateExpr(rt.Expr)),
      _ => throw new ArgumentOutOfRangeException(nameof(s))
    };
  }

  public AstEnvironment EvaluateStatements(IEnumerable<Statement> statements) =>
    statements.Aggregate(this, (env, s) => env.EvaluateStatement(s));

  private AstEnvironment Join(AstEnvironment other, Zen<bool> guard)
  {
    var e = new AstEnvironment();
    foreach (var (variable, value) in _env)
    {
      e = e.Update(variable, Zen.If(guard, value, other._env.ContainsKey(variable) ? other[variable] : null));
    }

    // add any variables that were not present in this but are in other
    foreach (var (variable, value) in other._env.Where(p => !_env.ContainsKey(p.Key)))
    {
      e = e.Update(variable, Zen.If(guard, null, value));
    }

    return e;
  }
}

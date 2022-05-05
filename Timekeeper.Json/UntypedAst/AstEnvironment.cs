using System.Collections.Immutable;
using Timekeeper.Json.UntypedAst.AstExpr;
using ZenLib;

namespace Timekeeper.Json.UntypedAst;

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

  private AstEnvironment With(string var, dynamic val)
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
      GetField getField =>
        EvaluateExpr(getField.record).GetField(getField.fieldName),
      // typeof(Zen<>).GetMethod("GetField")!.MakeGenericMethod(getField.recordTy, getField.fieldTy)
      // .Invoke(null, new object[] {EvaluateExpr(getField.record), getField.fieldName})!,
      Var v => this[v.Name],
      WithField withField => EvaluateExpr(withField.record)
        .WithField(withField.fieldName, EvaluateExpr(withField.fieldValue)),
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
      Assign a => With(a.Name, EvaluateExpr(a.Expr)),
      IfThenElse ite => EvaluateStatements(ite.ThenCase)
        .Join(EvaluateStatements(ite.ElseCase), EvaluateExpr(ite.Guard)),
      Return rt => With(ReturnValue, EvaluateExpr(rt.Expr)),
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
      e = e.With(variable, Zen.If(guard, value, other._env.ContainsKey(variable) ? other[variable] : null));
    }

    // add any variables that were not present in this but are in other
    foreach (var (variable, value) in other._env.Where(p => !_env.ContainsKey(p.Key)))
    {
      e = e.With(variable, Zen.If(guard, null, value));
    }

    return e;
  }
}

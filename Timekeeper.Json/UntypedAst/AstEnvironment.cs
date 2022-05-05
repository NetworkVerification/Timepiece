using System.Collections.Immutable;
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

  public object this[string var] => _env[var];

  public AstEnvironment() : this(ImmutableDictionary<string, dynamic>.Empty)
  {
  }

  public AstEnvironment With(string var, object val)
  {
    return new AstEnvironment(_env.SetItem(var, val));
  }

  private dynamic EvaluateExpr(Expr e)
  {
    return e switch
    {
      Call => throw new NotImplementedException(),
      ConstantExpr constant => Zen.Constant(constant.value),
      Var v => this[v.Name],
      Havoc => Zen.Symbolic<bool>(),
      UnaryOpExpr uoe => uoe.unaryOp(EvaluateExpr(uoe.expr)),
      BinaryOpExpr boe => boe.binaryOp(EvaluateExpr(boe.expr1), EvaluateExpr(boe.expr2)),
      _ => throw new ArgumentOutOfRangeException(nameof(e))
      // SetContains setContains => Set.Contains(EvaluateExpr2(setContains.expr2), EvaluateExpr2(setContains.expr1)),
    };
  }

  public AstEnvironment EvaluateStmt(Statement s)
  {
    return s switch
    {
      Assign a => With(a.Name, EvaluateExpr(a.Expr)),
      IfThenElse ite => EvaluateStmts(ite.ThenCase).Join(EvaluateStmts(ite.ElseCase), EvaluateExpr(ite.Guard)),
      Return rt => With(ReturnValue, EvaluateExpr(rt.Expr)),
      _ => throw new ArgumentOutOfRangeException(nameof(s))
    };
  }

  public AstEnvironment EvaluateStmts(IEnumerable<Statement> statements) =>
    statements.Aggregate(this, (env, s) => env.EvaluateStmt(s));

  private AstEnvironment Join(AstEnvironment other, Zen<bool> guard)
  {
    var d = ImmutableDictionary<string, dynamic>.Empty;
    foreach (var (variable, value) in _env)
    {
      d = d.SetItem(variable, Zen.If(guard, _env[variable], other[variable]));
    }
    // TODO: add in variables from other

    return new AstEnvironment(d);
  }
}

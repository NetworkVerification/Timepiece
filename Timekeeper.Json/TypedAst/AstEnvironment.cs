using System.Collections.Immutable;
using Timekeeper.Json.TypedAst.AstExpr;
using Timekeeper.Json.TypedAst.AstStmt;
using ZenLib;

namespace Timekeeper.Json.TypedAst;

public class AstEnvironment
{
  private readonly ImmutableDictionary<string, dynamic> _env;
  private const string ReturnValue = "##return##";

  public AstEnvironment(ImmutableDictionary<string, dynamic> env)
  {
    _env = env;
  }

  public object this[string var] => _env[var];

  public AstEnvironment() : this(ImmutableDictionary<string, dynamic>.Empty) { }

  public AstEnvironment With(string var, object val)
  {
    return new AstEnvironment(_env.SetItem(var, val));
  }

  private dynamic EvaluateExpr2(IExpr e)
  {
    return e switch
    {
      Var<dynamic> v => _env[v.Name],
      Havoc => Zen.Symbolic<bool>(),
      ConstantExpr<int> c => Zen.Constant(c.value),
      SetContains setContains => Set.Contains(EvaluateExpr2(setContains.expr2), EvaluateExpr2(setContains.expr1)),
      _ => throw new ArgumentOutOfRangeException(nameof(e))
    };
  }

  public AstEnvironment EvaluateStmt(IStatement s)
  {
    return s switch
    {
      Assign<dynamic> a => With(a.Var, EvaluateExpr2(a.Expr)),
      Skip => this,
      IfThenElse<dynamic> ite => EvaluateStmt(ite.TrueStatement).Join(EvaluateStmt(ite.FalseStatement), EvaluateExpr2(ite.Guard)),
      AstStmt.Seq<dynamic> seq => EvaluateStmt(seq.S1).EvaluateStmt(seq.S2),
      Return<dynamic> rt => With(ReturnValue, EvaluateExpr2(rt.Expr)),
      _ => throw new ArgumentOutOfRangeException(nameof(s))
    };
  }

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

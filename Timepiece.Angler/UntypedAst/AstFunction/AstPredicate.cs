using Timepiece.Angler.UntypedAst.AstExpr;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

/// <summary>
///   A unary function from type T to bool, aka a predicate over type T.
/// </summary>
public class AstPredicate
{
  public AstPredicate(string arg, Expr body)
  {
    Arg = arg;
    Body = body ?? throw new ArgumentNullException(nameof(body), "AstPredicate body must not be null.");
  }

  public string Arg { get; set; }
  public Expr Body { get; set; }

  public Func<Zen<RouteEnvironment>, Zen<bool>> Evaluate(AstEnvironment env)
  {
    return t =>
    {
      var astState = env.Update(Arg, t);
      return astState.EvaluateExpr(t, Body).Item2;
    };
  }
}

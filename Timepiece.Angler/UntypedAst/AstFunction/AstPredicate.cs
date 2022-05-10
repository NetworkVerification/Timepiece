using Timepiece.Angler.UntypedAst.AstExpr;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

/// <summary>
///   A unary function from type T to bool, aka a predicate over type T.
/// </summary>
/// <typeparam name="T">The predicate's argument type.</typeparam>
public class AstPredicate<T>
{
  public AstPredicate(string arg, Expr body)
  {
    Arg = arg;
    Body = body;
  }

  public string Arg { get; set; }
  public Expr Body { get; set; }

  public Func<Zen<T>, Zen<bool>> Evaluate()
  {
    return t =>
    {
      var astState = new AstEnvironment().Update(Arg, t);
      return astState.EvaluateExpr(Body);
    };
  }
}

using Gardener.AstExpr;
using ZenLib;

namespace Gardener;

/// <summary>
/// A unary function from type T to bool, aka a predicate over type T.
/// </summary>
/// <typeparam name="T">The predicate's argument type.</typeparam>
public class AstPredicate<T>
{
  public AstPredicate(string arg, Expr<bool, T> expr)
  {
    Arg = arg;
    Expr = expr;
  }

  public Expr<bool,T> Expr { get; set; }

  public string Arg { get; }

  public Func<Zen<T>, Zen<bool>> Evaluate(State<T> state)
  {
    state.Add(Arg, t => t);
    var finalState = Expr.Evaluate(state);
    return finalState;
  }
}

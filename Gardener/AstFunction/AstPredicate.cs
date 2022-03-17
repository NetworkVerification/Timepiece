using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstFunction;

/// <summary>
/// A unary function from type T to bool, aka a predicate over type T.
/// </summary>
/// <typeparam name="T">The predicate's argument type.</typeparam>
public class AstPredicate<T> : IRenameable
{
  public AstPredicate(string arg, Expr<bool, T> expr)
  {
    Arg = arg;
    Expr = expr;
  }

  public Expr<bool, T> Expr { get; set; }

  public string Arg { get; set; }

  public Func<Zen<T>, Zen<bool>> Evaluate(State<T> state)
  {
    state.Add(Arg, t => t);
    var finalState = Expr.Evaluate(state);
    return finalState;
  }

  public void Rename(string oldArg, string newArg)
  {
    if (Arg.Equals(oldArg))
    {
      Arg = newArg;
    }

    Expr.Rename(oldArg, newArg);
  }
}

public static class AstPredicateExtensions
{
  public static Func<Zen<T1>, Zen<T2>, Zen<bool>> EvaluateBinary<T1, T2>(this AstPredicate<Pair<T1, T2>> f,
    State<Pair<T1, T2>> state)
  {
    return (t1, t2) => f.Evaluate(state)(Pair.Create(t1, t2));
  }
}

using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstFunction;

/// <summary>
/// A unary function from type T to bool, aka a predicate over type T.
/// </summary>
/// <typeparam name="T">The predicate's argument type.</typeparam>
public class AstPredicate<T> : AstFunctionBase<T, Expr<bool, T>>, IEvaluable<T, bool>
{
  public AstPredicate(string arg, Expr<bool, T> expr) : base(arg, expr) { }

  public Func<Zen<T>, Zen<bool>> Evaluate(State<T> state)
  {
    state.Add(Arg, t => t);
    return Body.Evaluate(state);
  }
}

public static class AstPredicateExtensions
{
  public static Func<Zen<T1>, Zen<T2>, Zen<bool>> EvaluateBinary<T1, T2>(this AstPredicate<Pair<T1, T2>> f,
    State<Pair<T1, T2>> state)
  {
    var pairPredicate = f.Evaluate(state);
    return (t1, t2) => pairPredicate(Pair.Create(t1, t2));
  }
}

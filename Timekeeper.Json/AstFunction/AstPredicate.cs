using Timekeeper.Json.AstExpr;
using ZenLib;

namespace Timekeeper.Json.AstFunction;

/// <summary>
/// A unary function from type T to bool, aka a predicate over type T.
/// </summary>
/// <typeparam name="T">The predicate's argument type.</typeparam>
public class AstPredicate<T> : AstFunctionBase<T, Expr<bool, T>>, IEvaluable<T, bool>
{
  public AstPredicate(string arg, Expr<bool, T> expr) : base(arg, expr) { }

  public Func<Zen<T>, Zen<bool>> Evaluate(AstState<T> astState)
  {
    astState.Add(Arg, t => t);
    return Body.Evaluate(astState);
  }
}

public static class AstPredicateExtensions
{
  public static Func<Zen<T1>, Zen<T2>, Zen<bool>> EvaluateBinary<T1, T2>(this AstPredicate<Pair<T1, T2>> f,
    AstState<Pair<T1, T2>> astState)
  {
    var pairPredicate = f.Evaluate(astState);
    return (t1, t2) => pairPredicate(Pair.Create(t1, t2));
  }
}

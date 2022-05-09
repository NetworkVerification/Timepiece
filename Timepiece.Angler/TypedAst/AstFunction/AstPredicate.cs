using Timepiece.Angler.TypedAst.AstExpr;
using ZenLib;

namespace Timepiece.Angler.TypedAst.AstFunction;

/// <summary>
///   A unary function from type T to bool, aka a predicate over type T.
/// </summary>
/// <typeparam name="T">The predicate's argument type.</typeparam>
public class AstPredicate<T> : AstFunctionBase<T, Expr<bool>>
{
  public AstPredicate(string arg, Expr<bool> expr) : base(arg, expr)
  {
  }

  public Func<Zen<T>, Zen<bool>> Evaluate()
  {
    return t =>
    {
      var astState = new AstState();
      astState.Add(Arg, t);
      return Body.Evaluate(astState);
    };
  }
}

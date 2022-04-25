using System.Text.Json.Serialization;
using Timekeeper.Json.TypedAst.AstExpr;
using Timekeeper.Json.TypedAst.AstStmt;
using ZenLib;

namespace Timekeeper.Json.TypedAst.AstFunction;

/// <summary>
///   A unary function of type T to Option T.
/// </summary>
/// <typeparam name="T">The type of the function's input and output.</typeparam>
public class AstFunction<T> : AstFunctionBase<T, Statement<T, T>>
{
  [JsonConstructor]
  public AstFunction(string arg, Statement<T, T> body) : base(arg, body)
  {
  }

  /// <summary>
  ///   Generate an AstFunc that returns its argument unchanged.
  /// </summary>
  /// <typeparam name="T">The type of the argument.</typeparam>
  /// <returns>A function that returns an argument unchanged.</returns>
  public static AstFunction<T> Identity()
  {
    return new AstFunction<T>("x", new Return<T>(new Var<T>("x")));
  }

  /// <summary>
  ///   Return an AstFunc which is the equivalent of calling this function
  ///   and passing its returned value to the given function, i.e. composing the functions.
  /// </summary>
  /// <param name="that">A second AstFunc from TResult to TResult2.</param>
  /// <typeparam name="T">The final return type.</typeparam>
  /// <returns>A new AstFunc composing the behavior of the original two.</returns>
  public AstFunction<T> Compose(AstFunction<T> that)
  {
    // bind the result of this body to that argument
    var bound = Body.Bind(that.Arg);
    return new AstFunction<T>(Arg, new Seq<T, T>(bound, that.Body));
  }

  /// <summary>
  ///   Compose an enumerable of AstFunc which all have the same type.
  /// </summary>
  /// <param name="functions">An enumerable of functions to compose.</param>
  /// <param name="seed">A starting function for the composition.</param>
  /// <typeparam name="T">The type of the function's inputs and outputs.</typeparam>
  /// <returns>A final function representing the composition of all inputs.</returns>
  public static AstFunction<T> Compose(IEnumerable<AstFunction<T>> functions, AstFunction<T> seed)
  {
    return functions.Aggregate(seed, (current, ff) => current.Compose(ff));
  }

  public Func<Zen<T>, Zen<T>> Evaluate(AstState<T> astState)
  {
    astState.Add(Arg, t => t);
    var finalState = Body.Evaluate(astState);
    return finalState.Return ??
           throw new InvalidOperationException("No value returned by function.");
  }
}

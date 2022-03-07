using System.Text.Json.Serialization;
using ZenLib;

namespace Gardener;

/// <summary>
/// A unary function from a type TArg to a type TResult.
/// </summary>
/// <typeparam name="TArg">The type of the function's input.</typeparam>
/// <typeparam name="TResult">The type of the function's output.</typeparam>
public class AstFunc<TArg, TResult>
{
  /// <summary>
  /// The name of the argument to the function.
  /// </summary>
  public string Arg { get; set; }

  /// <summary>
  /// The body of the function.
  /// </summary>
  public Statement<TResult> Body { get; set; }

  [JsonConstructor]
  public AstFunc(string arg, Statement<TResult> body)
  {
    Arg = arg;
    Body = body;
  }

  public static AstFunc<T, T> Identity<T>()
  {
    return new AstFunc<T, T>("x", new Return<T>(new Var<T>("x")));
  }


  /// <summary>
  /// Return an AstFunc which is the equivalent of calling this function
  /// and passing its returned value to the given function, i.e. composing the functions.
  /// </summary>
  /// <param name="that">A second AstFunc from TResult to TResult2.</param>
  /// <typeparam name="TResult2">The final return type.</typeparam>
  /// <returns>A new AstFunc composing the behavior of the original two.</returns>
  public AstFunc<TArg, TResult2> Compose<TResult2>(AstFunc<TResult, TResult2> that)
  {
    // bind the result of this body to that argument
    var bound = Body.Bind(that.Arg);
    return new AstFunc<TArg, TResult2>(Arg, new Seq<TResult2>(bound, that.Body));
  }

  /// <summary>
  /// Compose an enumerable of AstFunc which all have the same type.
  /// </summary>
  /// <param name="functions"></param>
  /// <typeparam name="TT"></typeparam>
  /// <returns></returns>
  public static AstFunc<TT, TT> Compose<TT>(IEnumerable<AstFunc<TT, TT>> functions)
  {
    var f = Identity<TT>();
    return functions.Aggregate(f, (current, ff) => current.Compose(ff));
  }

  public Func<Zen<TArg>, Zen<TResult>> Evaluate(State state)
  {
    state.Add<TArg>(Arg, t => t);
    var finalState = Body.Evaluate(state);
    return finalState.Return as Func<Zen<TArg>, Zen<TResult>> ??
           throw new InvalidOperationException("No value returned by function.");
  }
}

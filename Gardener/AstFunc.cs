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
  public List<Statement> Body { get; set; }

  public Expr<TResult> Return { get; set; }

  [JsonConstructor]
  public AstFunc(string arg, List<Statement> body, Expr<TResult> returnExpr)
  {
    Arg = arg;
    Body = body;
    Return = returnExpr;
  }

  public static AstFunc<T, T> Identity<T>()
  {
    return new AstFunc<T, T>("x", new List<Statement>(), new Var<T>("x"));
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
    var assignResult = new Assign<TResult>(that.Arg, Return);
    return new AstFunc<TArg, TResult2>(Arg, Body.Concat(that.Body.Prepend(assignResult)).ToList(), that.Return);
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
    var finalState = Body.Aggregate(state, (s, stmt) => stmt.Evaluate(s));
    return Return.Evaluate<TArg>(finalState);
    // return finalState.Return as Func<Zen<TArg>, Zen<TResult>> ?? throw new InvalidOperationException("No value returned by function.");
  }
}

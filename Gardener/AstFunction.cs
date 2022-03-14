using System.Text.Json.Serialization;
using Gardener.AstExpr;
using Gardener.AstStmt;
using ZenLib;

namespace Gardener;

/// <summary>
/// A unary function of type T to T.
/// </summary>
/// <typeparam name="T">The type of the function's input and output.</typeparam>
public class AstFunction<T> : IRenameable
{
  /// <summary>
  /// The name of the argument to the function.
  /// </summary>
  public string Arg { get; set; }

  /// <summary>
  /// The body of the function.
  /// </summary>
  public Statement<T, T> Body { get; set; }

  [JsonConstructor]
  public AstFunction(string arg, Statement<T, T> body)
  {
    Arg = arg;
    Body = body;
  }

  public void Rename(string oldArg, string newArg)
  {
    if (Arg.Equals(oldArg))
    {
      Arg = newArg;
    }
    Body.Rename(oldArg, newArg);
  }

  /// <summary>
  /// Generate an AstFunc that returns its argument unchanged.
  /// </summary>
  /// <typeparam name="T">The type of the argument.</typeparam>
  /// <returns>A function that returns an argument unchanged.</returns>
  public static AstFunction<T> Identity()
  {
    return new AstFunction<T>("x", new Return<T>(new Var<T>("x")));
  }

  /// <summary>
  /// Return an AstFunc which is the equivalent of calling this function
  /// and passing its returned value to the given function, i.e. composing the functions.
  /// </summary>
  /// <param name="that">A second AstFunc from TResult to TResult2.</param>
  /// <typeparam name="T">The final return type.</typeparam>
  /// <returns>A new AstFunc composing the behavior of the original two.</returns>
  public AstFunction<T> Compose(AstFunction<T> that)
  {
    // bind the result of this body to that argument
    // FIXME: if this.Arg and that.Arg are equal, the behavior will diverge!
    // TODO: check if this.Arg and that.Arg are equal, and rename that.Arg if so
    var bound = Body.Bind(that.Arg);
    return new AstFunction<T>(Arg, new Seq<T, T>(bound, that.Body));
  }

  /// <summary>
  /// Compose an enumerable of AstFunc which all have the same type.
  /// </summary>
  /// <param name="functions"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static AstFunction<T> Compose(IEnumerable<AstFunction<T>> functions)
  {
    var f = Identity();
    return functions.Aggregate(f, (current, ff) => current.Compose(ff));
  }

  public Func<Zen<T>, Zen<T>> Evaluate(State<T> state)
  {
    state.Add(Arg, t => t);
    var finalState = Body.Evaluate(state);
    return finalState.Return ??
           throw new InvalidOperationException("No value returned by function.");
  }
}

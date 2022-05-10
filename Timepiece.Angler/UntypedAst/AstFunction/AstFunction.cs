using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

/// <summary>
///   A unary function of type T to Option T.
/// </summary>
/// <typeparam name="T">The type of the function's input and output.</typeparam>
public class AstFunction<T>
{
  [JsonConstructor]
  public AstFunction(string arg, IEnumerable<Statement> body)
  {
    Arg = arg;
    Body = body;
  }

  public string Arg { get; set; }
  public IEnumerable<Statement> Body { get; set; }

  public void Rename(string oldArg, string newArg)
  {
    if (Arg.Equals(oldArg)) Arg = newArg;
    foreach (var b in Body)
    {
      b.Rename(oldArg, newArg);
    }
  }

  /// <summary>
  ///   Generate an AstFunc that returns its argument unchanged.
  /// </summary>
  /// <typeparam name="T">The type of the argument.</typeparam>
  /// <returns>A function that returns an argument unchanged.</returns>
  public static AstFunction<T> Identity()
  {
    return new AstFunction<T>("x", new List<Statement> {new Return(new Var("x"))});
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
    var bound = Body.Select(s => s.Bind(that.Arg));
    return new AstFunction<T>(Arg, bound.Concat(that.Body));
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

  public Func<Zen<T>, Zen<T>> Evaluate()
  {
    return t =>
    {
      var env = new AstEnvironment(ImmutableDictionary<string, dynamic>.Empty.Add(Arg, t));
      return (Zen<T>) (env.EvaluateStatements(Body).Return() ??
                       throw new InvalidOperationException("No value returned by function."));
    };
  }
}

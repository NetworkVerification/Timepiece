using Newtonsoft.Json;
using Timepiece.Angler.UntypedAst.AstStmt;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

/// <summary>
///   A unary function of type T to Option T.
/// </summary>
/// <typeparam name="T">The type of the function's input and output.</typeparam>
public class AstFunction<T>
{
  [System.Text.Json.Serialization.JsonConstructor]
  public AstFunction(string arg, IEnumerable<Statement> body)
  {
    Arg = arg;
    Body = body;
  }

  [JsonProperty(Required = Required.Always)]
  public string Arg { get; set; }

  [JsonProperty(Required = Required.Always)]
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
  /// Compose another AstFunction f onto this one: add f's body after this function's body,
  /// and replace all references to f's argument with references to this function's argument.
  /// </summary>
  /// <param name="f">The function to compose with this AstFunction.</param>
  /// <returns>void</returns>
  public void Compose(AstFunction<T> f)
  {
    Body = Body.Concat(f.Body);
    // replace all references to f's argument in f2 with this function's argument.
    // TODO: condition the subsequent function on the result of the first body
    Rename(f.Arg, Arg);
  }

  /// <summary>
  ///   Generate an AstFunc that returns its argument unchanged.
  /// </summary>
  /// <typeparam name="T">The type of the argument.</typeparam>
  /// <returns>A function that returns an argument unchanged.</returns>
  public static AstFunction<T> Identity()
  {
    return new AstFunction<T>("x", new List<Statement>());
  }

  public Func<Zen<T>, Zen<T>> Evaluate(AstEnvironment<T> environment)
  {
    return t =>
    {
      var env = environment.Update(Arg, t);
      return (Zen<T>) (env.EvaluateStatements(Body)[Arg] ??
                       throw new InvalidOperationException("No value returned by function."));
    };
  }
}

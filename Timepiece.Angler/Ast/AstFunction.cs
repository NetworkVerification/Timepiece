using Newtonsoft.Json;
using Timepiece.Angler.Ast.AstStmt;

namespace Timepiece.Angler.Ast;

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
    foreach (var b in Body) b.Rename(oldArg, newArg);
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
}

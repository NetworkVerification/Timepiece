using Newtonsoft.Json;
using Timepiece.Angler.Ast.AstStmt;

namespace Timepiece.Angler.Ast;

/// <summary>
///   A unary function of type T to Option T.
/// </summary>
/// <typeparam name="T">The type of the function's input and output.</typeparam>
public record AstFunction<T>(string Arg, IEnumerable<Statement> Body)
{
  [JsonProperty(Required = Required.Always)]
  public string Arg { get; set; } = Arg;

  [JsonProperty(Required = Required.Always)]
  public IEnumerable<Statement> Body { get; set; } = Body;

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

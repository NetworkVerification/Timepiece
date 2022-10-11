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

  public Func<Zen<T>, Zen<T>> Evaluate(AstEnvironment environment)
  {
    return t =>
    {
      var env = environment.Update(Arg, t);
      return (Zen<T>) (env.EvaluateStatements(Body).Return() ??
                       throw new InvalidOperationException("No value returned by function."));
    };
  }
}

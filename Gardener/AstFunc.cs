using ZenLib;

namespace Gardener;

public class AstFunc
{
  /// <summary>
  /// The name of the function.
  /// </summary>
  public string Name { get; set; }
  /// <summary>
  /// The list of arguments to the function.
  /// </summary>
  public string[] Args { get; set; }
  /// <summary>
  /// The body of the function.
  /// </summary>
  public Statement Body { get; set; }

  public AstFunc(string name, string[] args, Statement body)
  {
    Name = name;
    Args = args;
    Body = body;
  }

  public Func<Zen<dynamic>, Zen<dynamic>> ToZenUnary()
  {
    CheckArgsLength(1);
    var initialState = new State(Args);
    var finalState = Body.Evaluate(initialState);
    // FIXME: return a function that maps an argument to the constraint on the final state
    return x => finalState[Args[0]];
  }

  public Func<Zen<dynamic>, Zen<dynamic>, Zen<dynamic>> ToZenBinary()
  {
    CheckArgsLength(2);
    var initialState = new State(Args);
    var finalState = Body.Evaluate(initialState);
    // FIXME: return a function that maps the arguments to the constraints on the final state
    return (x, y) => Zen.And(finalState[Args[0]], finalState[Args[1]]);
  }

  private void CheckArgsLength(uint numberArgs)
  {
    if (Args.Length != numberArgs)
    {
      throw new ArgumentException("Invalid number of arguments.");
    }
  }
}

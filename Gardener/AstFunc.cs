using ZenLib;

namespace Gardener;

public class AstFunc
{
  /// <summary>
  /// The list of arguments to the function.
  /// </summary>
  public string[] Args { get; set; }
  /// <summary>
  /// The body of the function.
  /// </summary>
  public Statement Body { get; set; }

  public AstFunc(string[] args, Statement body)
  {
    Args = args;
    Body = body;
  }

  public dynamic ToZenUnary()
  {
    CheckArgsLength(1);
    var initialState = new State(Args);
    var finalState = Body.Evaluate(initialState);
    // FIXME: don't use Args[0] necessarily: it should depend on what return does
    return finalState[Args[0]];
  }

  public dynamic ToZenBinary()
  {
    CheckArgsLength(2);
    var initialState = new State(Args);
    var finalState = Body.Evaluate(initialState);
    // FIXME: return a function that maps the arguments to the final state's return
    throw new NotImplementedException();
  }

  private void CheckArgsLength(uint numberArgs)
  {
    if (Args.Length != numberArgs)
    {
      throw new ArgumentException("Invalid number of arguments.");
    }
  }
}

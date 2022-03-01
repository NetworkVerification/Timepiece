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
    throw new NotImplementedException();
  }

  public Func<Zen<dynamic>, Zen<dynamic>, Zen<dynamic>> ToZenBinary()
  {
    throw new NotImplementedException();
  }
}

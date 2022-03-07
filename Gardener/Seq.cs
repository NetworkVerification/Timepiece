using ZenLib;

namespace Gardener;

/// <summary>
/// A sequence of two statements.
/// The first statement must have a type of Unit,
/// while the second may have any return type.
/// </summary>
/// <typeparam name="T">The second statement's return type.</typeparam>
public class Seq<T> : Statement<T>
{
  public Seq(Statement<Unit> first, Statement<T> second)
  {
    First = first;
    Second = second;
  }

  public Statement<Unit> First { get; set; }
  public Statement<T> Second { get; set; }

  public override State Evaluate(State state)
  {
    return Second.Evaluate(First.Evaluate(state));
  }

  public override Statement<Unit> Bind(string var)
  {
    return new Seq<Unit>(First.Bind(var), Second.Bind(var));
  }
}

using ZenLib;

namespace Gardener.AstStmt;

/// <summary>
/// A sequence of two statements.
/// The first statement must have a type of Unit,
/// while the second may have any return type.
/// </summary>
/// <typeparam name="T">The second statement's return type.</typeparam>
/// <typeparam name="TState">The type of evaluation state for this statement.</typeparam>
public class Seq<T, TState> : Statement<T, TState>
{
  public Seq(Statement<Unit, TState> first, Statement<T, TState> second)
  {
    First = first;
    Second = second;
  }

  public Statement<Unit, TState> First { get; set; }
  public Statement<T, TState> Second { get; set; }

  public override State<TState> Evaluate(State<TState> state)
  {
    return Second.Evaluate(First.Evaluate(state));
  }

  public override Statement<Unit, TState> Bind(string var)
  {
    return new Seq<Unit, TState>(First.Bind(var), Second.Bind(var));
  }

  public override void Rename(string oldVar, string newVar)
  {
    First.Rename(oldVar, newVar);
    Second.Rename(oldVar, newVar);
  }
}

using ZenLib;

namespace Gardener.AstStmt;

public class Skip<T> : Statement<Unit, T>
{
  public override State<T> Evaluate(State<T> state)
  {
    return state;
  }

  public override Statement<Unit, T> Bind(string var)
  {
    return this;
  }

  public override void Rename(string oldVar, string newVar)
  {
    ; // no-op
  }
}

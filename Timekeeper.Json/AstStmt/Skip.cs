using ZenLib;

namespace Timekeeper.Json.AstStmt;

public class Skip<T> : Statement<Unit, T>
{
  public override AstState<T> Evaluate(AstState<T> astState)
  {
    return astState;
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

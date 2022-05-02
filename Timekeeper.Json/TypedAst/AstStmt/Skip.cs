using ZenLib;

namespace Timekeeper.Json.TypedAst.AstStmt;

public class Skip : Statement<Unit>
{
  public override AstState Evaluate<TS>(AstState astState)
  {
    return astState;
  }

  public override Statement<Unit> Bind(string var)
  {
    return this;
  }

  public override void Rename(string oldVar, string newVar)
  {
    ; // no-op
  }
}

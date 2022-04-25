namespace Timekeeper.Json.UntypedAst;

public class Skip : Statement
{
  public Skip()
  {

  }

  public override void Rename(string oldVar, string newVar)
  {
    // no-op
    ;
  }
}

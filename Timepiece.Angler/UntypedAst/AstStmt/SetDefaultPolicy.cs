namespace Timepiece.Angler.UntypedAst.AstStmt;

public class SetDefaultPolicy : Statement
{
  public string PolicyName { get; set; }

  public SetDefaultPolicy(string policyName)
  {
    PolicyName = policyName;
  }

  public override void Rename(string oldVar, string newVar)
  {
    if (PolicyName == oldVar)
    {
      PolicyName = newVar;
    }
  }
}

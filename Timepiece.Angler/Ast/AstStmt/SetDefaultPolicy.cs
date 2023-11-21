namespace Timepiece.Angler.Ast.AstStmt;

public record SetDefaultPolicy : Statement
{
  public SetDefaultPolicy(string policyName)
  {
    PolicyName = policyName;
  }

  public string PolicyName { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    if (PolicyName == oldVar) PolicyName = newVar;
  }
}

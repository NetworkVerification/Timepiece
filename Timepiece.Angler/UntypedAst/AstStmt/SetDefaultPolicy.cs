namespace Timepiece.Angler.UntypedAst.AstStmt;

public class SetDefaultPolicy : Statement
{
  public string Name { get; set; }

  public SetDefaultPolicy(string name)
  {
    Name = name;
  }

  public override void Rename(string oldVar, string newVar)
  {
    if (Name == oldVar)
    {
      Name = newVar;
    }
  }
}

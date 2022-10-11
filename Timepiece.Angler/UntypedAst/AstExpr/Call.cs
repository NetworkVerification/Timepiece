namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Call : Expr
{
  public Call(string name)
  {
    Name = name;
  }

  public string Name { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    if (Name == oldVar)
    {
      Name = newVar;
    }
  }
}

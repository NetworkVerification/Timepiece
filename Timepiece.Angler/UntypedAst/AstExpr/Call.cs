namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Call : Expr
{
  public Call(string name, string arg)
  {
    Name = name;
    Arg = arg;
  }

  public string Name { get; set; }
  public string Arg { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    if (Name == oldVar)
    {
      Name = newVar;
    }

    if (Arg == oldVar)
    {
      Arg = newVar;
    }
  }
}

namespace Timekeeper.Json.UntypedAst;

public class Call : Expr
{
  public Call(string name, Expr[] arguments)
  {
    Name = name;
    Arguments = arguments;
  }

  public string Name { get; set; }

  public Expr[] Arguments { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    if (Name == oldVar)
    {
      Name = newVar;
    }

    foreach (var argument in Arguments)
    {
      argument.Rename(oldVar, newVar);
    }
  }
}

namespace Timepiece.Angler.Ast.AstExpr;

public record Call : Expr
{
  public Call(string name)
  {
    Name = name;
  }

  public string Name { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    if (Name == oldVar) Name = newVar;
  }
}

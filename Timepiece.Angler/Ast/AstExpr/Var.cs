namespace Timepiece.Angler.Ast.AstExpr;

public record Var : Expr
{
  public Var(string name)
  {
    Name = name;
  }

  public string Name { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    if (oldVar == Name) Name = newVar;
  }
}

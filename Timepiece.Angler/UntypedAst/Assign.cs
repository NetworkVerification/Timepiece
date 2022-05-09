using Timepiece.Angler.UntypedAst.AstExpr;

namespace Timepiece.Angler.UntypedAst;

public class Assign : Statement
{
  public Assign(string name, Expr expr)
  {
    Name = name;
    Expr = expr;
  }

  public string Name { get; set; }
  public Expr Expr { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Expr.Rename(oldVar, newVar);
    if (Name == oldVar)
    {
      Name = newVar;
    }
  }
}

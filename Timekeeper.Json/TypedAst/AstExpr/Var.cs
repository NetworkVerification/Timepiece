using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class Var<T> : Expr<T>
{
  public Var(string name)
  {
    Name = name;
  }

  public string Name { get; set; }

  public override Zen<T> Evaluate(AstState astState)
  {
    if (!astState.ContainsVar(Name))
      throw new ArgumentOutOfRangeException($"Variable {Name} unbound in the given astState.");

    return astState[Name];
  }

  public override void Rename(string oldVar, string newVar)
  {
    if (Name.Equals(oldVar)) Name = newVar;
  }
}

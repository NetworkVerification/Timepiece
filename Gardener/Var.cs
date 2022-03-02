using ZenLib;

namespace Gardener;

public class Var : Expr
{
  public string Name { get; set; }

  public Var(string name)
  {
    Name = name;
  }


  public override Zen<T> ToZen<T>()
  {
    throw new NotImplementedException();
  }

  public override Zen<bool> Evaluate(State state)
  {
    if (!state.ContainsVar(Name))
    {
      throw new ArgumentOutOfRangeException($"Variable {Name} unbound in the given state.");
    }
    return state[Name];
  }
}

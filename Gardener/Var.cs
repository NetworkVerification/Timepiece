using ZenLib;

namespace Gardener;

public class Var<T> : Expr<T, T>
{
  public string Name { get; set; }

  public Var(string name)
  {
    Name = name;
  }

  public override Func<Zen<T>, Zen<T>> Evaluate(State<T> state)
  {
    if (!state.ContainsVar(Name))
    {
      throw new ArgumentOutOfRangeException($"Variable {Name} unbound in the given state.");
    }
    return state[Name];
  }
}

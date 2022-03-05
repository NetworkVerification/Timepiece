using ZenLib;

namespace Gardener;

public class Var<T> : Expr<T>
{
  public string Name { get; set; }

  public Var(string name)
  {
    Name = name;
  }


  public override Zen<T> ToZen()
  {
    throw new NotImplementedException();
  }

  public override Func<Zen<TInput>, Zen<T>> Evaluate<TInput>(State state)
  {
    if (!state.ContainsVar(Name))
    {
      throw new ArgumentOutOfRangeException($"Variable {Name} unbound in the given state.");
    }
    return (Func<Zen<TInput>, Zen<T>>) state[Name];
  }
}

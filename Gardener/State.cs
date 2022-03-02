using System.Reflection.Metadata;
using ZenLib;

namespace Gardener;

public class State
{
  /// <summary>
  /// A mapping from variable names to their constraints.
  /// </summary>
  private Dictionary<string, Zen<bool>> Variables { get; }

  public State(IEnumerable<string> args)
  {
    Variables = new Dictionary<string, Zen<bool>>();
    foreach (var arg in args)
    {
      Variables.Add(arg, Zen.Symbolic<bool>());
    }
  }

  public bool ContainsVar(string var)
  {
    return Variables.ContainsKey(var);
  }

  public Zen<bool> this[string var]
  {
    get => Variables[var];
    private set => Variables[var] = value;
  }

  public void Add<T>(string var, T val)
  {
    Variables.Add(var, Zen.Eq(Zen.Symbolic<T>(), Zen.Constant(val)));
  }

  /// <summary>
  /// Join two states according to the boolean expression guard.
  /// The value of each variable is defined according to the guard:
  /// when it is true, this state's value holds;
  /// when it is false, the other state's value holds.
  /// </summary>
  /// <param name="other">Another State to use.</param>
  /// <param name="guard">A boolean expression acting as the guard.</param>
  public void Join(State other, Zen<bool> guard)
  {
    foreach (var (key, value) in Variables)
    {
      this[key] = Zen.Implies(guard, value);
    }
    foreach (var (key, value) in other.Variables)
    {
      var impl = Zen.Implies(Zen.Not(guard), value);
      if (ContainsVar(key))
      {
        this[key] = Zen.And(this[key], impl);
      }
      else
      {
        this[key] = impl;
      }
    }
  }
}

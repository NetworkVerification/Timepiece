using System.Reflection.Metadata;
using ZenLib;

namespace Gardener;

public class State
{
  /// <summary>
  /// A mapping from variable names to functions.
  /// </summary>
  private Dictionary<string, object> Variables { get; }

  public State(IEnumerable<string> args)
  {
    Variables = new Dictionary<string, object>();
    foreach (var arg in args)
    {
      Variables.Add(arg, new Func<object, object>(t => t));
    }
  }

  public bool ContainsVar(string var)
  {
    return Variables.ContainsKey(var);
  }

  public object this[string var]
  {
    get => Variables[var];
    private set => Variables[var] = value;
  }

  public void Add<T>(string var, Func<T, T> val)
  {
    Variables.Add(var, val);
  }

  /// <summary>
  /// Join two states according to the boolean expression guard.
  /// The value of each variable is defined according to the guard:
  /// when it is true, this state's value holds;
  /// when it is false, the other state's value holds.
  /// </summary>
  /// <param name="other">Another State to use.</param>
  /// <param name="guard">A boolean expression acting as the guard.</param>
  public void Join(State other, object guard)
  {
    // make both states have the same keys
    foreach (var key in Variables.Keys.Where(key => !other.ContainsVar(key)))
    {
      other.Add<object>(key, t => t);
    }
    foreach (var key in other.Variables.Keys.Where(key => !ContainsVar(key)))
    {
      Add<object>(key, t => t);
    }

    // foreach (var (key, value) in Variables)
    // {
    // this[key] = Zen.Implies(guard, value);
    // }
    foreach (var (key, value) in other.Variables)
    {
      var g = (Func<dynamic, bool>) guard;
      var trueCase = (Func<dynamic, dynamic>) this[key];
      var falseCase = (Func<dynamic, dynamic>) value;
      if (ContainsVar(key))
      {
        this[key] = new Func<object, dynamic>(t => Zen.If(g(t),
          trueCase(t),
          falseCase(t)));
      }
      else
      {
        this[key] = value;
      }
    }
  }
}

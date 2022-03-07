using System.Reflection.Metadata;
using ZenLib;

namespace Gardener;

public class State
{
  /// <summary>
  /// A mapping from variable names to values.
  /// </summary>
  private Dictionary<string, object> Variables { get; }

  public object? Return { get; set; }

  /// <summary>
  /// Return a new state with no bindings.
  /// </summary>
  public State()
  {
    Variables = new Dictionary<string, object>();
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

  /// <summary>
  /// Bind the given variable in the state to the given function.
  /// </summary>
  /// <param name="var">The name of the variable.</param>
  /// <param name="val">Its unary function to be bound to.</param>
  /// <typeparam name="T">The type of the function's argument and return type.</typeparam>
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
  public void Join(State other, Func<Zen<object>, Zen<bool>> guard)
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

    foreach (var (key, value) in other.Variables)
    {
      var trueCase = (Func<dynamic, dynamic>) this[key];
      var falseCase = (Func<dynamic, dynamic>) value;
      if (ContainsVar(key))
      {
        this[key] = new Func<object, dynamic>(t => Zen.If(guard(t),
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

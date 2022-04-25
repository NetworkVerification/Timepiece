using ZenLib;

namespace Gardener;

public class AstState<T>
{
  /// <summary>
  /// A mapping from variable names to values.
  /// </summary>
  private Dictionary<string, Func<Zen<T>, Zen<T>>> Variables { get; }

  public Func<Zen<T>, Zen<T>>? Return { get; set; }

  public bool Debug { get; }

  /// <summary>
  /// Return a new state with no bindings.
  /// </summary>
  public AstState()
  {
    Variables = new Dictionary<string, Func<Zen<T>, Zen<T>>>();
  }

  public AstState(bool debug)
  {
    Variables = new Dictionary<string, Func<Zen<T>, Zen<T>>>();
    Debug = debug;
  }


  public bool ContainsVar(string var)
  {
    return Variables.ContainsKey(var);
  }

  public Func<Zen<T>, Zen<T>> this[string var]
  {
    get => Variables[var];
    set => Variables[var] = value;
  }

  /// <summary>
  /// Bind the given variable in the state to the given function.
  /// </summary>
  /// <param name="var">The name of the variable.</param>
  /// <param name="val">Its unary function to be bound to.</param>
  /// <typeparam name="T">The type of the function's argument and return type.</typeparam>
  public void Add(string var, Func<Zen<T>, Zen<T>> val)
  {
    Variables.Add(var, val);
    if (Debug)
    {
      Console.WriteLine($"Added {var} bound to {val}");
    }
  }

  /// <summary>
  /// Join two states according to the boolean expression guard.
  /// The value of each variable is defined according to the guard:
  /// when it is true, this state's value holds;
  /// when it is false, the other state's value holds.
  /// </summary>
  /// <param name="other">Another AstState to use.</param>
  /// <param name="guard">A boolean expression acting as the guard.</param>
  public void Join(AstState<T> other, Func<Zen<T>, Zen<bool>> guard)
  {
    // make both states have the same keys
    foreach (var key in Variables.Keys.Where(key => !other.ContainsVar(key)))
    {
      other.Add(key, t => t);
    }
    foreach (var key in other.Variables.Keys.Where(key => !ContainsVar(key)))
    {
      Add(key, t => t);
    }

    foreach (var (key, value) in other.Variables)
    {
      var trueCase = this[key];
      var falseCase = value;
      if (ContainsVar(key))
      {
        this[key] = (t => Zen.If(guard(t),
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

using ZenLib;

namespace Timekeeper.Json.TypedAst;

public class AstState
{
  /// <summary>
  ///   Return a new state with no bindings.
  /// </summary>
  public AstState()
  {
    Variables = new Dictionary<string, object>();
  }

  /// <summary>
  ///   A mapping from variable names to values.
  /// </summary>
  private Dictionary<string, object> Variables { get; }

  public object? Return { get; set; }

  public object this[string var]
  {
    get => Variables[var];
    set => Variables[var] = value;
  }


  public bool ContainsVar(string var)
  {
    return Variables.ContainsKey(var);
  }

  /// <summary>
  ///   Bind the given variable in the state to the given function.
  /// </summary>
  /// <param name="var">The name of the variable.</param>
  /// <param name="val">Its unary function to be bound to.</param>
  public void Add(string var, object val)
  {
    Variables.Add(var, val);
  }

  /// <summary>
  ///   Join two states according to the boolean expression guard.
  ///   The value of each variable is defined according to the guard:
  ///   when it is true, this state's value holds;
  ///   when it is false, the other state's value holds.
  /// </summary>
  /// <param name="other">Another AstState to use.</param>
  /// <param name="guard">A boolean expression acting as the guard.</param>
  public void Join<T>(AstState other, Func<Zen<T>, Zen<bool>> guard)
  {
    // make both states have the same keys
    foreach (var key in Variables.Keys.Where(key => !other.ContainsVar(key)))
      other.Add(key, new Func<Zen<T>, Zen<T>>(t => t));

    foreach (var key in other.Variables.Keys.Where(key => !ContainsVar(key)))
      Add(key, new Func<Zen<T>, Zen<T>>(t => t));

    foreach (var (key, value) in other.Variables)
    {
      var trueCase = (Func<Zen<T>, Zen<T>>) this[key];
      var falseCase = (Func<Zen<T>, Zen<T>>) value;
      this[key] = new Func<Zen<T>, Zen<T>>(t => Zen.If(guard(t), trueCase(t), falseCase(t)));
    }
  }
}

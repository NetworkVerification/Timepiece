using System;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui;

/// <summary>
/// A symbolic value with an associated name, for ease of reference.
/// </summary>
/// <typeparam name="T">The symbolic type associated with the value.</typeparam>
public class SymbolicValue<T>
{
  public SymbolicValue(string name)
  {
    Name = name;
    Value = Symbolic<T>();
  }

  public SymbolicValue(string name, Func<Zen<T>, Zen<bool>> constraint)
  {
    Name = name;
    Value = Symbolic<T>();
    Constraint = constraint;
  }

  public Func<Zen<T>, Zen<bool>> Constraint { get; set; }

  /// <summary>
  /// The internal Zen symbolic.
  /// </summary>
  public Zen<T> Value { get; }

  /// <summary>
  /// The name used to refer to the symbolic.
  /// </summary>
  public string Name { get; }

  public bool HasConstraint()
  {
    return Constraint != null;
  }

  public Zen<bool> Encode()
  {
    return HasConstraint() ? Constraint(Value) : True();
  }
}
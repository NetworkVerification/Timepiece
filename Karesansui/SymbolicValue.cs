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

  /// <summary>
  /// Return true if the symbolic value equals the given constant value.
  /// </summary>
  /// <param name="s">A symbolic value.</param>
  /// <param name="val">A constant value of the same type as the symbolic value.</param>
  /// <returns>True if the values are the same, and false otherwise.</returns>
  public static Zen<bool> operator ==(SymbolicValue<T> s, T val)
  {
    return s != null ? Constant(val) == s.Value : False();
  }

  /// <summary>
  /// Return true if the symbolic value does not equal the given constant value.
  /// </summary>
  /// <param name="s">A symbolic value.</param>
  /// <param name="val">A constant value of the same type.</param>
  /// <returns>True if the values are different, and false otherwise.</returns>
  public static Zen<bool> operator !=(SymbolicValue<T> s, T val)
  {
    return Not(s == val);
  }

  public bool HasConstraint()
  {
    return Constraint != null;
  }

  public Zen<bool> Encode()
  {
    return HasConstraint() ? Constraint(Value) : True();
  }
}
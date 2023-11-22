#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenLib;
using ZenLib.ModelChecking;
using static ZenLib.Zen;

namespace Timepiece;

/// <summary>
///   A symbolic value with an associated name, for ease of reference.
/// </summary>
/// <typeparam name="T">The symbolic type associated with the value.</typeparam>
public record SymbolicValue<T> : ISymbolic
{
  public SymbolicValue(string name)
  {
    Name = name;
    Value = Symbolic<T>($"{name}");
  }

  public SymbolicValue(string name, Func<Zen<T>, Zen<bool>> constraint)
  {
    Name = name;
    Value = Symbolic<T>($"{name}");
    Constraint = constraint;
  }

  public Func<Zen<T>, Zen<bool>>? Constraint { get; set; }

  /// <summary>
  ///   The internal Zen symbolic.
  /// </summary>
  public Zen<T> Value { get; }

  /// <summary>
  ///   The name used to refer to the symbolic.
  /// </summary>
  public string Name { get; }

  public bool HasConstraint()
  {
    return Constraint != null;
  }

  public Zen<bool> Encode()
  {
    return HasConstraint() ? Constraint!(Value) : True();
  }

  public object? GetSolution(ZenSolution model)
  {
    return model.Get(Value);
  }

  public string SolutionToString(ZenSolution model)
  {
    return $"symbolic {Name} := {model.Get(Value)}";
  }

  /// <summary>
  ///   Return true if the symbolic value equals the given constant value.
  /// </summary>
  /// <param name="val">A constant value of the same type as the symbolic value.</param>
  /// <returns>True if the values are the same, and false otherwise.</returns>
  public Zen<bool> EqualsValue(T val)
  {
    return Constant(val) == Value;
  }

  /// <summary>
  ///   Return true if the symbolic value does not equal the given constant value.
  /// </summary>
  /// <param name="val">A constant value of the same type.</param>
  /// <returns>True if the values are different, and false otherwise.</returns>
  public Zen<bool> DoesNotEqualValue(T val)
  {
    return Not(EqualsValue(val));
  }
}

public static class SymbolicValue
{
  /// <summary>
  ///   Assign a fresh symbolic variable for each of the given <paramref name="keys"/>.
  ///   Use the given <paramref name="namePrefix"/> to name the variable.
  ///   If a <paramref name="constraint"/> is given, apply it to every symbolic variable.
  /// </summary>
  /// <param name="namePrefix">a string prefix for the symbolic variable names</param>
  /// <param name="keys">the keys to create symbolic routes for (i.e. the keys to the dictionary)</param>
  /// <param name="constraint">a predicate over <c>RouteEnvironment</c>s</param>
  /// <returns>a dictionary from keys to symbolic variables</returns>
  public static Dictionary<TKey, SymbolicValue<TValue>> SymbolicDictionary<TKey, TValue>(string namePrefix,
    IEnumerable<TKey> keys, Func<Zen<TValue>, Zen<bool>>? constraint = null) where TKey : notnull
  {
    return keys.ToDictionary(e => e, e => constraint is null
      ? new SymbolicValue<TValue>($"{namePrefix}-{e}")
      : new SymbolicValue<TValue>($"{namePrefix}-{e}", constraint));
  }
}

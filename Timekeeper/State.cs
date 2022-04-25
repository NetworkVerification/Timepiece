using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Timekeeper.Datatypes;
using ZenLib;

namespace Timekeeper;

public class State<T, TS>
{
  public readonly Dictionary<string, T> nodeStates;
  private readonly Option<(string, T)> _focusedNode = Option.None<(string, T)>();
  public readonly Option<BigInteger> time;
  public readonly Dictionary<string, TS> symbolicStates;
  public readonly SmtCheck check;

  /// <summary>
  /// Reconstruct the network state from the given model, focused on the given node.
  /// </summary>
  /// <param name="model">The ZenSolution returned by the solver.</param>
  /// <param name="node">The node this solution pertains to.</param>
  /// <param name="route">The Zen variable referring to this node's route.</param>
  /// <param name="time">A specific time this solution pertains to, or None if the time is irrelevant.</param>
  /// <param name="symbolics">The symbolic values bound in the solution.</param>
  /// <param name="check">Which check led to the generation of this state.</param>
  public State(ZenSolution model, string node, Zen<T> route, Option<Zen<BigInteger>> time,
    IEnumerable<SymbolicValue<TS>> symbolics, SmtCheck check)
  {
    this.check = check;
    this.time = time.Select(model.Get);
    nodeStates = new Dictionary<string, T> {{node, model.Get(route)}};
    symbolicStates = symbolics.ToDictionary(symbol => symbol.Name, symbol => model.Get(symbol.Value));
  }

  /// <summary>
  /// Reconstruct the network state from the given inductive check model, focused on the given node and its neighbors.
  /// </summary>
  /// <param name="model">The ZenSolution returned by the solver.</param>
  /// <param name="node">The node this solution pertains to.</param>
  /// <param name="neighborStates">The Zen variables referring to this node's neighbors' routes.</param>
  /// <param name="time">A specific time this solution pertains to, or None if the time is irrelevant.</param>
  /// <param name="symbolics">The symbolic values bound in the solution.</param>
  public State(ZenSolution model, string node, Zen<T> nodeRoute,
    IEnumerable<KeyValuePair<string, Zen<T>>> neighborStates,
    Zen<BigInteger> time, IEnumerable<SymbolicValue<TS>> symbolics)
  {
    check = SmtCheck.Inductive;
    this.time = Option.Some(model.Get(time));
    _focusedNode = Option.Some((node, model.Get(nodeRoute)));
    nodeStates = neighborStates.ToDictionary(p => p.Key, p => model.Get(p.Value));
    symbolicStates = symbolics.ToDictionary(symbol => symbol.Name, symbol => model.Get(symbol.Value));
  }

  /// <summary>
  /// Reconstruct the network state from the given monolithic check model for all given nodes.
  /// </summary>
  /// <param name="model">The ZenSolution returned by the solver.</param>
  /// <param name="nodeStates">The Zen variables referring to each node and its route.</param>
  /// <param name="symbolics">The symbolic values bound in the solution.</param>
  public State(ZenSolution model, IEnumerable<KeyValuePair<string, Zen<T>>> nodeStates,
    IEnumerable<SymbolicValue<TS>> symbolics)
  {
    check = SmtCheck.Monolithic;
    this.nodeStates = nodeStates.ToDictionary(p => p.Key, p => model.Get(p.Value));
    symbolicStates = symbolics.ToDictionary(symbol => symbol.Name, symbol => model.Get(symbol.Value));
  }

  public override string ToString()
  {
    var sb = new StringBuilder();
    foreach (var (name, value) in symbolicStates)
    {
      sb.AppendLine($"symbolic {name} := {value}");
    }

    time.May(t => sb.Append($"at time = {t}").AppendLine());

    if (_focusedNode.HasValue)
    {
      var (focus, focusRoute) = _focusedNode.Value;
      sb.AppendLine($"node {focus} had route := {focusRoute}");
    }

    foreach (var (node, route) in nodeStates)
    {
      var specifier = _focusedNode.HasValue ? $"neighbor {node} of {_focusedNode.Value.Item1}" : $"node {node}";
      sb.AppendLine($"{specifier} had route := {route}");
    }

    return sb.ToString();
  }

  /// <summary>
  /// Print the state to the console, identifying which check fails to hold for this state.
  /// </summary>
  public void ReportCheckFailure()
  {
    Console.ForegroundColor = ConsoleColor.Red;
    var whichCheck = check switch
    {
      SmtCheck.Base => "Base",
      SmtCheck.Monolithic => "Monolithic",
      SmtCheck.Inductive => "Inductive",
      SmtCheck.Safety => "Safety",
      _ => throw new ArgumentOutOfRangeException()
    };
    Console.WriteLine($"{whichCheck} check failed!");
    Console.WriteLine(ToString());
    Console.ResetColor();
  }
}

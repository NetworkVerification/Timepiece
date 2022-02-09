using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ZenLib;

namespace Karesansui;

public class State<T, TS>
{
  public Dictionary<string, T> nodeStates;
  private Option<string> _focusedNode = Option.None<string>();
  public Option<BigInteger> time;
  public Dictionary<string, TS> symbolicStates;

  /// <summary>
  /// Reconstruct the network state from the given model, focused on the given node.
  /// </summary>
  /// <param name="model">The ZenSolution returned by the solver.</param>
  /// <param name="node">The node this solution pertains to.</param>
  /// <param name="route">The Zen variable referring to this node's route.</param>
  /// <param name="time">A specific time this solution pertains to, or None if the time is irrelevant.</param>
  /// <param name="symbolics">The symbolic values bound in the solution.</param>
  public State(ZenSolution model, string node, Zen<T> route, Option<Zen<BigInteger>> time,
    IEnumerable<SymbolicValue<TS>> symbolics)
  {
    this.time = time.Select(model.Get);
    nodeStates = new Dictionary<string, T> {{node, model.Get(route)}};
    symbolicStates = symbolics.ToDictionary(symbol => symbol.Name, symbol => model.Get(symbol.Value));
  }

  /// <summary>
  /// Reconstruct the network state from the given model, focused on the given node and its neighbors.
  /// </summary>
  /// <param name="model">The ZenSolution returned by the solver.</param>
  /// <param name="node">The node this solution pertains to.</param>
  /// <param name="neighborStates">The Zen variables referring to this node's neighbors' routes.</param>
  /// <param name="time">A specific time this solution pertains to, or None if the time is irrelevant.</param>
  /// <param name="symbolics">The symbolic values bound in the solution.</param>
  public State(ZenSolution model, string node, IEnumerable<KeyValuePair<string, Zen<T>>> neighborStates,
    Option<Zen<BigInteger>> time, IEnumerable<SymbolicValue<TS>> symbolics)
  {
    this.time = time.Select(model.Get);
    _focusedNode = Option.Some(node);
    nodeStates = neighborStates.ToDictionary(p => p.Key, p => model.Get(p.Value));
    symbolicStates = symbolics.ToDictionary(symbol => symbol.Name, symbol => model.Get(symbol.Value));
  }

  /// <summary>
  /// Reconstruct the network state from the given model for all given nodes.
  /// </summary>
  /// <param name="model">The ZenSolution returned by the solver.</param>
  /// <param name="nodeStates">The Zen variables referring to each node and its route.</param>
  /// <param name="time">A specific time this solution pertains to, or None if the time is irrelevant.</param>
  /// <param name="symbolics">The symbolic values bound in the solution.</param>
  public State(ZenSolution model, IEnumerable<KeyValuePair<string, Zen<T>>> nodeStates, Option<Zen<BigInteger>> time,
    IEnumerable<SymbolicValue<TS>> symbolics)
  {
    this.time = time.Select(model.Get);
    this.nodeStates = nodeStates.ToDictionary(p => p.Key, p => model.Get(p.Value));
    symbolicStates = symbolics.ToDictionary(symbol => symbol.Name, symbol => model.Get(symbol.Value));
  }

  public override string ToString()
  {
    var sb = new StringBuilder();
    foreach (var (name, value) in symbolicStates)
    {
      sb.Append($"symbolic {name} := {value}\n");
    }

    time.May(t => sb.Append($"At time = {t}\n"));

    foreach (var (node, route) in nodeStates)
    {
      var specifier = _focusedNode.HasValue ? $"neighbor {node} of {_focusedNode.Value}" : "node";
      sb.Append($"{specifier} had route := {route}\n");
    }

    return sb.ToString();
  }

  /// <summary>
  /// Print the state to the console, identifying which check fails to hold for this state.
  /// </summary>
  /// <param name="check">A string specifying the failing check (monolithic, base, assertion or inductive).</param>
  public void ReportCheckFailure(string check)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  {check} check failed!");
    Console.WriteLine(ToString());
    Console.ResetColor();
  }
}

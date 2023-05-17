using System;
using System.Collections.Generic;
using System.Linq;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   Represents a network.
/// </summary>
/// <typeparam name="T">The type of the routes.</typeparam>
/// <typeparam name="TS">The type of symbolic values associated with the network.</typeparam>
public class Network<T, TS>
{
  /// <summary>
  ///   The topology of the network.
  /// </summary>
  public Topology Topology { get; }

  /// <summary>
  ///   The transfer function for each edge.
  /// </summary>
  public Dictionary<(string, string), Func<Zen<T>, Zen<T>>> TransferFunction { get; protected init; }

  /// <summary>
  ///   The merge function for routes.
  /// </summary>
  public Func<Zen<T>, Zen<T>, Zen<T>> MergeFunction { get; }

  /// <summary>
  ///   The initial values for each node.
  /// </summary>
  public Dictionary<string, Zen<T>> InitialValues { get; protected init; }

  /// <summary>
  ///   Any symbolics on the network's components.
  /// </summary>
  public SymbolicValue<TS>[] Symbolics { get; set; }

  public Network(Topology topology, Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction, Dictionary<string, Zen<T>> initialValues,
    SymbolicValue<TS>[] symbolics)
  {
    Topology = topology;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitialValues = initialValues;
    Symbolics = symbolics;
  }

  /// <summary>
  ///   Return a route corresponding to the application of one step of the network semantics:
  ///   starting from the initial route at a node, merge in each transferred route from the node's neighbor.
  /// </summary>
  /// <param name="node">The focal node.</param>
  /// <param name="routes">The routes of all nodes in the network.</param>
  /// <returns>A route.</returns>
  protected Zen<T> UpdateNodeRoute(string node, IReadOnlyDictionary<string, Zen<T>> routes)
  {
    return Topology[node].Aggregate(InitialValues[node],
      (current, predecessor) =>
        MergeFunction(current, TransferFunction[(predecessor, node)](routes[predecessor])));
  }

  /// <summary>
  ///   Return the conjunction of all constraints over symbolic values given to the network.
  /// </summary>
  /// <returns>A Zen boolean.</returns>
  protected Zen<bool> GetSymbolicConstraints()
  {
    var assumptions = Symbolics.Where(p => p.HasConstraint()).Select(p => p.Encode()).ToArray();
    // And() with an empty array throws an error, so we check the length first
    return assumptions.Length == 0 ? Zen.True() : Zen.And(assumptions);
  }
}

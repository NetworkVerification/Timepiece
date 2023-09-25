using System;
using System.Collections.Generic;
using System.Linq;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   Represents a network.
/// </summary>
/// <typeparam name="RouteType">The type of the routes.</typeparam>
/// <typeparam name="NodeType">The type of nodes.</typeparam>
/// <typeparam name="SymbolicType">The type of symbolic values associated with the network.</typeparam>
public class Network<RouteType, NodeType, SymbolicType>
{
  /// <summary>
  ///   The topology of the network.
  /// </summary>
  public Digraph<NodeType> Digraph { get; }

  /// <summary>
  ///   The transfer function for each edge.
  /// </summary>
  public Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> TransferFunction
  {
    get;
    protected init;
  }

  /// <summary>
  ///   The merge function for routes.
  /// </summary>
  public Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> MergeFunction { get; }

  /// <summary>
  ///   The initial values for each node.
  /// </summary>
  public Dictionary<NodeType, Zen<RouteType>> InitialValues { get; protected init; }

  /// <summary>
  ///   Any symbolics on the network's components.
  /// </summary>
  public SymbolicValue<SymbolicType>[] Symbolics { get; set; }

  /// <summary>
  /// Construct a new <c>Network</c>.
  /// </summary>
  /// <param name="digraph">a network topology</param>
  /// <param name="transferFunction">a dictionary from edges to transfer functions</param>
  /// <param name="mergeFunction">a function for merging two routes</param>
  /// <param name="initialValues">a dictionary from nodes to initial routes</param>
  /// <param name="symbolics">an array of symbolic values</param>
  public Network(Digraph<NodeType> digraph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunction,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<NodeType, Zen<RouteType>> initialValues,
    SymbolicValue<SymbolicType>[] symbolics)
  {
    Digraph = digraph;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitialValues = initialValues;
    Symbolics = symbolics;
  }

  /// <summary>
  ///   Return a route corresponding to the application of one step of the network semantics:
  ///   starting from the initial route at a node, merge in each transferred route from the node's neighbors.
  /// </summary>
  /// <param name="node">The focal node.</param>
  /// <param name="routes">The routes of all nodes in the network.</param>
  /// <returns>A route.</returns>
  public Zen<RouteType> UpdateNodeRoute(NodeType node, IReadOnlyDictionary<NodeType, Zen<RouteType>> routes)
  {
    return Digraph[node].Aggregate(InitialValues[node],
      (current, predecessor) =>
        MergeFunction(current, TransferFunction[(predecessor, node)](routes[predecessor])));
  }

  /// <inheritdoc cref="UpdateNodeRoute(NodeType,System.Collections.Generic.IReadOnlyDictionary{NodeType,ZenLib.Zen{RouteType}})"/>
  /// <param name="neighbors">The specific neighbors to transfer from: must be a subset (or equal to) the actual
  /// set of neighbors, otherwise an exception will be raised.</param>
  /// <returns>A route.</returns>
  public Zen<RouteType> UpdateNodeRoute(NodeType node, IReadOnlyDictionary<NodeType, Zen<RouteType>> routes,
    IEnumerable<NodeType> neighbors)
  {
    return neighbors.Aggregate(InitialValues[node],
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

using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace Timepiece;

/// <summary>
/// Representation of a symbolically-chosen destination top-of-rack node in a particular fat-tree pod.
/// </summary>
public class SymbolicDestination : SymbolicValue<Pair<string, int>>
{
  public SymbolicDestination(NodeLabelledDigraph<string, int> digraph) : base("dest",
    p => digraph.ExistsNode(n => Zen.And(n.IsEdge(), DestEquals(p, digraph, n))))
  {
    Node = Value.Item1();
    Pod = Value.Item2();
  }

  /// <summary>
  ///   The pod of the symbolic destination.
  /// </summary>
  public Zen<int> Pod { get; set; }

  /// <summary>
  ///   The node name of the symbolic destination.
  /// </summary>
  public Zen<string> Node { get; set; }

  /// <summary>
  ///   Return a concrete integer representing the (possibly-symbolic) distance between a node and a destination edge-layer node
  ///   in a fat-tree topology.
  /// </summary>
  /// <param name="node">The given node.</param>
  /// <param name="nodePod">The given node's pod.</param>
  /// <returns></returns>
  public Zen<BigInteger> SymbolicDistance(string node, int nodePod)
  {
    // cases for when the destination is an edge node
    return Zen.If(Node == node, BigInteger.Zero,
      Zen.If(Zen.And(node.IsAggregation(), Pod == nodePod), new BigInteger(1),
        Zen.If(Zen.And(node.IsAggregation(), Pod != nodePod), new BigInteger(3),
          Zen.If<BigInteger>(Zen.And(node.IsEdge(), Pod != nodePod), new BigInteger(4),
            new BigInteger(2)))));
  }

  /// <summary>
  /// Return a <i>symbolic</i> integer representing the distance between a node and a destination edge-layer node
  /// in a fat-tree topology.
  /// The integer is chosen according to its position in the given <c>symbolicTimes</c> list,
  /// which must have exactly 5 elements.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="nodePod"></param>
  /// <param name="symbolicTimes"></param>
  /// <returns></returns>
  public Zen<BigInteger> SymbolicDistanceCases(string node, int nodePod, IReadOnlyList<Zen<BigInteger>> symbolicTimes)
  {
    if (symbolicTimes.Count != 5)
      throw new ArgumentOutOfRangeException(nameof(symbolicTimes),
        $"Received symbolic times {symbolicTimes}, expecting 5 elements!");
    // cases for when the destination is an edge node
    return Zen.If(Node == node, symbolicTimes[0],
      Zen.If(Zen.And(node.IsAggregation(), Pod == nodePod), symbolicTimes[1],
        Zen.If(Zen.And(node.IsAggregation(), Pod != nodePod), symbolicTimes[3],
          Zen.If(Zen.And(node.IsEdge(), Pod != nodePod), symbolicTimes[4],
            symbolicTimes[2]))));
  }

  /// <summary>
  ///   Return a Zen boolean constraint indicating that the symbolic destination equals the given node in the given
  ///   labelled topology.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="node"></param>
  /// <returns></returns>
  public Zen<bool> Equals(NodeLabelledDigraph<string, int> digraph, string node)
  {
    return DestEquals(Value, digraph, node);
  }

  private static Zen<bool> DestEquals(Zen<Pair<string, int>> p, NodeLabelledDigraph<string, int> digraph, string node)
  {
    return Zen.And(p.Item1() == node, p.Item2() == digraph.L(node));
  }
}

public static class TopologyExtensions
{
  /// <summary>
  ///   A lifted version of topology.Nodes.Any over Zen booleans.
  ///   Return Zen.True() if there exists a node in the topology satisfying the predicate,
  ///   and Zen.False() otherwise.
  /// </summary>
  /// <param name="digraph">The network topology.</param>
  /// <param name="predicate">The Zen predicate over nodes.</param>
  /// <returns>A Zen boolean.</returns>
  public static Zen<bool> ExistsNode(this Digraph<string> digraph, Func<string, Zen<bool>> predicate)
  {
    return digraph.FoldNodes(Zen.False(), (disjuncts, n) => Zen.Or(disjuncts, predicate(n)));
  }

  /// <summary>
  ///   A lifted version of topology.Nodes.All over Zen booleans.
  ///   Return Zen.True() if every node in the topology satisfying the predicate,
  ///   and Zen.False() otherwise.
  /// </summary>
  /// <param name="digraph">The network topology.</param>
  /// <param name="predicate">The Zen predicate over nodes.</param>
  /// <returns>A Zen boolean.</returns>
  public static Zen<bool> ForAllNodes(this Digraph<string> digraph, Func<string, Zen<bool>> predicate)
  {
    return digraph.FoldNodes(Zen.True(), (conjuncts, n) => Zen.And(conjuncts, predicate(n)));
  }
}

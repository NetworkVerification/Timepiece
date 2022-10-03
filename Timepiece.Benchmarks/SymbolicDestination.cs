using System.Numerics;
using ZenLib;

namespace Timepiece.Benchmarks;

public class SymbolicDestination : SymbolicValue<Pair<string, int>>
{
  public SymbolicDestination(LabelledTopology<int> topology) : base("dest",
    p => topology.ExistsNode(n => Zen.And(n.IsEdge(), DestEquals(p, topology, n))))
  {
    Node = Value.Item1();
    Pod = Value.Item2();
  }

  /// <summary>
  /// The pod of the symbolic destination.
  /// </summary>
  public Zen<int> Pod { get; set; }

  /// <summary>
  /// The node name of the symbolic destination.
  /// </summary>
  public Zen<string> Node { get; set; }

  /// <summary>
  /// Return an integer representing the (possibly-symbolic) distance between a node and a destination edge-layer node
  /// in a fattree topology.
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
  /// Return a Zen boolean constraint indicating that the symbolic destination equals the given node in the given
  /// labelled topology.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="node"></param>
  /// <returns></returns>
  public Zen<bool> Equals(LabelledTopology<int> topology, string node) =>
    DestEquals(Value, topology, node);

  private static Zen<bool> DestEquals(Zen<Pair<string, int>> p, LabelledTopology<int> topology, string node) =>
    Zen.And(p.Item1() == node, p.Item2() == topology.L(node));
}

public static class TopologyExtensions {
/// <summary>
  /// A lifted version of topology.Nodes.Any over Zen booleans.
  /// Return Zen.True() if there exists a node in the topology satisfying the predicate,
  /// and Zen.False() otherwise.
  /// </summary>
  /// <param name="topology">The network topology.</param>
  /// <param name="predicate">The Zen predicate over nodes.</param>
  /// <returns>A Zen boolean.</returns>
  public static Zen<bool> ExistsNode(this Topology topology, Func<string, Zen<bool>> predicate)
  {
    return topology.FoldNodes(Zen.False(), (disjuncts, n) => Zen.Or(disjuncts, predicate(n)));
  }

/// <summary>
/// A lifted version of topology.Nodes.All over Zen booleans.
/// Return Zen.True() if every node in the topology satisfying the predicate,
/// and Zen.False() otherwise.
/// </summary>
/// <param name="topology">The network topology.</param>
/// <param name="predicate">The Zen predicate over nodes.</param>
/// <returns>A Zen boolean.</returns>
public static Zen<bool> ForAllNodes(this Topology topology, Func<string, Zen<bool>> predicate)
{
  return topology.FoldNodes(Zen.True(), (conjuncts, n) => Zen.And(conjuncts, predicate(n)));
}
}

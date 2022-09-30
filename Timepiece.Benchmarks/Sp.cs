using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

public class Sp<TS> : Network<Option<BatfishBgpRoute>, TS>
{
  /// <summary>
  /// Construct a verifiable network performing shortest-path routing to a single given destination.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="destination"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology topology, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    this(topology,
      topology.MapNodes(n =>
        n == destination ? Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()) : Option.None<BatfishBgpRoute>()),
      annotations, stableProperties, safetyProperties, new BigInteger(4), symbolics)
  {
  }

  /// <summary>
  /// Construct a verifiable network performing shortest-path routing; the initial values and convergence time
  /// must be given.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="initialValues"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="convergeTime"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology topology,
    Dictionary<string, Zen<Option<BatfishBgpRoute>>> initialValues,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime,
    SymbolicValue<TS>[] symbolics) : base(topology,
    topology.MapEdges(_ => Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath)),
    Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
    initialValues,
    annotations, stableProperties, safetyProperties, convergeTime, symbolics)
  {
  }
}

/// <summary>
/// Static factory class for Sp networks.
/// </summary>
public static class Sp
{
  /// <summary>
  /// Return an Sp k-fattree network to check reachability of a single destination node, where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  public static Sp<Unit> Reachability(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var reachable = Lang.IsSome<BatfishBgpRoute>();
    var annotations =
      distances.Select(p => (p.Key, Lang.Finally<Option<BatfishBgpRoute>>(p.Value, Option.IsSome)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties = topology.MapNodes(_ => reachable);
    // no safety property
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      System.Array.Empty<SymbolicValue<Unit>>());
  }

  // slightly weaker path length property with simpler annotations
  public static Sp<Unit> PathLengthNoSafety(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.OrSome<BatfishBgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(BatfishBgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.MapNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      System.Array.Empty<SymbolicValue<Unit>>());
  }

  /// <summary>
  /// Return an Sp k-fattree network to check path length of routes to a single destination node,
  /// where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  public static Sp<Unit> PathLength(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.IsNone<BatfishBgpRoute>(),
          Lang.IfSome(BatfishBgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.MapNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ =>
      Lang.Union(Lang.IsNone<BatfishBgpRoute>(), Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      System.Array.Empty<SymbolicValue<Unit>>());
  }

  /// <summary>
  /// Return a symbolic value representing a node in the given topology.
  /// The value is a pair with two components: the node, and the node's label in the topology
  /// (equivalent to calling topology.L with the node).
  /// </summary>
  /// <param name="topology">A labelled topology.</param>
  /// <returns>A symbolic value.</returns>
  public static SymbolicValue<Pair<string, int>> SymbolicDestination(LabelledTopology<int> topology) =>
    new("dest", p => topology.ExistsNode(n => Zen.And(n.IsEdge(), DestEquals(p, topology, n))));

  /// <summary>
  /// Return an integer representing the (possibly-symbolic) distance between a node and a destination edge-layer node
  /// in a fattree topology.
  /// </summary>
  /// <param name="node">The given node.</param>
  /// <param name="destination">The destination node.</param>
  /// <param name="samePods">Whether or not the node and destination node are in the same pod.</param>
  /// <returns></returns>
  private static Zen<BigInteger> SymbolicDistance(string node, Zen<string> destination, Zen<bool> samePods)
  {
    // check that either the destination or the node satisfy the given relation
    // cases for when the destination is an edge node
    return Zen.If(destination == node, BigInteger.Zero,
      Zen.If(Zen.And(node.IsAggregation(), samePods), new BigInteger(1),
        Zen.If(Zen.And(node.IsAggregation(), Zen.Not(samePods)), new BigInteger(3),
          Zen.If<BigInteger>(Zen.And(node.IsEdge(), Zen.Not(samePods)), new BigInteger(4),
            new BigInteger(2)))));
  }

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

  // helper function for checking a node is equal to the destination pair
  public static Zen<bool> DestEquals(Zen<Pair<string, int>> dest, LabelledTopology<int> topology, string node) =>
    Zen.And(dest.Item1() == node, dest.Item2() == topology.L(node));

  public static Sp<Pair<string, int>> AllPairsReachability(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BatfishBgpRoute>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var dest = SymbolicDestination(topology);
    var annotations = topology.MapNodes(n =>
      Lang.Finally(SymbolicDistance(n, dest.Value.Item1(), topology.L(n) == dest.Value.Item2()),
        Lang.IsSome<BatfishBgpRoute>()));
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()).Where(_ => DestEquals(dest.Value, topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new[] {dest});
  }

  public static Sp<Pair<string, int>> AllPairsPathLength(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ =>
      Lang.Union(b => b.IsNone(),
        Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    var dest = SymbolicDestination(topology);
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = SymbolicDistance(n, dest.Value.Item1(), topology.L(n) == dest.Value.Item2());
        return Lang.Until(distance,
          Lang.IsNone<BatfishBgpRoute>(),
          Lang.IfSome<BatfishBgpRoute>(b => Zen.And(b.LengthAtMost(distance), b.LpEquals(100))));
      });
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()).Where(_ => DestEquals(dest.Value, topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new[] {dest});
  }
}

using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

public class Sp<TS> : Network<Option<BatfishBgpRoute>, TS>
{
  public Sp(Topology topology, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    this(topology,
      topology.ForAllNodes(n =>
        n == destination ? Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()) : Option.None<BatfishBgpRoute>()),
      annotations, stableProperties, safetyProperties, new BigInteger(4), symbolics)
  {
  }

  public Sp(Topology topology,
    Dictionary<string, Zen<Option<BatfishBgpRoute>>> initialValues,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime,
    SymbolicValue<TS>[] symbolics) : base(topology,
    topology.ForAllEdges(_ => Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath)),
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
  public static Sp<Unit> Reachability(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var reachable = Lang.IsSome<BatfishBgpRoute>();
    var annotations =
      distances.Select(p => (p.Key, Lang.Finally<Option<BatfishBgpRoute>>(p.Value, Option.IsSome)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties = topology.ForAllNodes(_ => reachable);
    // no safety property
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
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
      topology.ForAllNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      System.Array.Empty<SymbolicValue<Unit>>());
  }

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
      topology.ForAllNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.ForAllNodes(_ =>
      Lang.Union(Lang.IsNone<BatfishBgpRoute>(), Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      System.Array.Empty<SymbolicValue<Unit>>());
  }

  private static Zen<BigInteger> ApproximateDistance(LabelledTopology<int> topology, string node,
    SymbolicValue<Pair<string, int>> dest)
  {
    var destNode = dest.Value.Item1();
    var destPod = dest.Value.Item2();
    var nodePod = Zen.Constant(topology.L(node));
    // check that either the destination or the node satisfy the given relation
    return Zen.If(destNode == Zen.Constant(node), BigInteger.Zero,
      Zen.If(Zen.And(node.IsAggregation(), destPod == nodePod), new BigInteger(5),
        Zen.If(Zen.And(node.IsAggregation(), destPod != nodePod), new BigInteger(15),
          Zen.If<BigInteger>(Zen.And(node.IsEdge(), destPod != nodePod), new BigInteger(20),
            new BigInteger(10))
        )
      )
    );
  }

  public static Sp<Pair<string, int>> AllPairsReachability(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.ForAllNodes(_ => Lang.IsSome<BatfishBgpRoute>());
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    // dest must be an edge node in the network with the appropriate pod number
    var dest = new SymbolicValue<Pair<string, int>>("dest",
      p =>
        topology.FoldNodes(Zen.False(),
          (disjuncts, n) => n.IsEdge()
            ? Zen.Or(disjuncts,
              Zen.And(p.Item1() == Zen.Constant(n), p.Item2() == Zen.Constant(topology.L(n))))
            : disjuncts));
    var annotations = topology.ForAllNodes(n =>
      Lang.Finally(ApproximateDistance(topology, n, dest), Lang.IsSome<BatfishBgpRoute>()));
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.ForAllNodes(n =>
        Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()).Where(_ =>
          Zen.And(dest.Value.Item1() == Zen.Constant(n), dest.Value.Item2() == Zen.Constant(topology.L(n)))));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new[] {dest});
  }
}

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
    var annotations =
      distances.Select(p => (p.Key, Lang.Finally<Option<BatfishBgpRoute>>(p.Value, Option.IsSome)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BatfishBgpRoute>());
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

  public static Sp<Pair<string, int>> AllPairsReachability(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BatfishBgpRoute>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var dest = new SymbolicDestination(topology);
    var annotations = topology.MapNodes(n =>
      Lang.Finally(dest.SymbolicDistance(n, topology.L(n)), Lang.IsSome<BatfishBgpRoute>()));
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()).Where(_ => dest.Equals(topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new SymbolicValue<Pair<string, int>>[] {dest});
  }

  public static Sp<Pair<string, int>> AllPairsPathLength(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ =>
      Lang.Union(b => b.IsNone(),
        Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    var dest = new SymbolicDestination(topology);
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance,
          Lang.IsNone<BatfishBgpRoute>(),
          Lang.IfSome(BatfishBgpRouteExtensions.MaxLengthDefaultLp(distance)));
      });
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()).Where(_ => dest.Equals(topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new SymbolicValue<Pair<string, int>>[] {dest});
  }

  public static Sp<Pair<string, int>> AllPairsPathLengthNoSafety(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var dest = new SymbolicDestination(topology);
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance,
          Lang.OrSome<BatfishBgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(BatfishBgpRouteExtensions.MaxLengthDefaultLp(distance)));
      });
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()).Where(_ => dest.Equals(topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new SymbolicValue<Pair<string, int>>[] {dest});
  }
}

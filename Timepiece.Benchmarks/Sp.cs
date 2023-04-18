using System.Numerics;
using Timepiece.Networks;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Benchmarks;

public class Sp<TS> : Network<Option<BgpRoute>, TS>
{
  /// <summary>
  ///   Construct a verifiable network performing shortest-path routing to a single given destination.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="destination"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology topology, string destination,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    this(topology,
      topology.MapNodes(n =>
        n == destination ? Option.Create<BgpRoute>(new BgpRoute()) : Option.None<BgpRoute>()),
      annotations, stableProperties, safetyProperties, new BigInteger(4), symbolics)
  {
  }

  /// <summary>
  ///   Construct a verifiable network performing shortest-path routing; the initial values and convergence time
  ///   must be given.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="initialValues"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="convergeTime"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology topology,
    Dictionary<string, Zen<Option<BgpRoute>>> initialValues,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime,
    SymbolicValue<TS>[] symbolics) : base(topology,
    topology.MapEdges(_ => Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath)),
    Lang.Omap2<BgpRoute>(BgpRouteExtensions.Min),
    initialValues,
    annotations, stableProperties, safetyProperties, convergeTime, symbolics)
  {
  }
}

/// <summary>
///   Static factory class for Sp networks.
/// </summary>
public static class Sp
{
  /// <summary>
  ///   Return an Sp k-fattree network to check reachability of a single destination node, where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  public static Sp<Unit> Reachability(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var annotations =
      distances.Select(p => (p.Key, Lang.Finally<Option<BgpRoute>>(p.Value, Option.IsSome)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    // no safety property
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  // slightly weaker path length property with simpler annotations
  public static Sp<Unit> PathLengthNoSafety(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.OrSome<BgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.MapNodes(_ => Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  /// <summary>
  ///   Return an Sp k-fattree network to check path length of routes to a single destination node,
  ///   where k is the given numPods.
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
          Lang.IsNone<BgpRoute>(),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.MapNodes(_ => Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ =>
      Lang.Union(Lang.IsNone<BgpRoute>(), Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  public static Sp<Pair<string, int>> AllPairsReachability(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var dest = new SymbolicDestination(topology);
    var annotations = topology.MapNodes(n =>
      Lang.Finally(dest.SymbolicDistance(n, topology.L(n)), Lang.IsSome<BgpRoute>()));
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new SymbolicValue<Pair<string, int>>[] {dest});
  }

  public static Sp<Pair<string, int>> AllPairsPathLength(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ =>
      Lang.Union(b => b.IsNone(),
        Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    var dest = new SymbolicDestination(topology);
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance,
          Lang.IsNone<BgpRoute>(),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(distance)));
      });
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new SymbolicValue<Pair<string, int>>[] {dest});
  }

  public static Sp<Pair<string, int>> AllPairsPathLengthNoSafety(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var dest = new SymbolicDestination(topology);
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance,
          Lang.OrSome<BgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(distance)));
      });
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    return new Sp<Pair<string, int>>(topology, initialValues, annotations, stableProperties, safetyProperties,
      new BigInteger(20),
      new SymbolicValue<Pair<string, int>>[] {dest});
  }
}

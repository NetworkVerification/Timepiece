using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

public class SimpleSp<TS> : Network<Option<SimpleBgpRoute>, TS>
{
  public SimpleSp(Topology topology, string destination,
    Dictionary<string, Func<Zen<Option<SimpleBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<SimpleBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<SimpleBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) : base(topology,
    topology.ForAllEdges(_ => Lang.Omap<SimpleBgpRoute, SimpleBgpRoute>(SimpleBgpRouteExtensions.IncrementAsPath)),
    Lang.Omap2<SimpleBgpRoute>(SimpleBgpRouteExtensions.Min),
    topology.ForAllNodes(n =>
      n == destination ? Option.Create<SimpleBgpRoute>(new SimpleBgpRoute()) : Option.None<SimpleBgpRoute>()),
    annotations, stableProperties, safetyProperties, new BigInteger(4), symbolics)
  {
  }

  // base(topology,
  // topology.ForAllEdges(_ => Lang.Omap<SimpleBgpRoute, SimpleBgpRoute>(SimpleBgpRouteExtensions.IncrementAsPath)),
  // Lang.Omap2<SimpleBgpRoute>(SimpleBgpRouteExtensions.Min),
  // topology.ForAllNodes(n => n == destination ? Option.Create<SimpleBgpRoute>(new SimpleBgpRoute()) : Option.None<SimpleBgpRoute>()),
  // annotations, stableProperties, safetyProperties, symbolics)
}

/// <summary>
/// Static factory class for Sp networks.
/// </summary>
public static class SimpleSp
{
  public static SimpleSp<Unit> Reachability(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var reachable = Lang.IsSome<SimpleBgpRoute>();
    var annotations =
      distances.Select(p => (p.Key, Lang.Finally(p.Value, reachable)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties = topology.ForAllNodes(_ => reachable);
    // no safety property
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<SimpleBgpRoute>>());
    return new SimpleSp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  // slightly weaker path length property with simpler annotations
  public static SimpleSp<Unit> PathLengthNoSafety(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.OrSome<SimpleBgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(SimpleBgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.ForAllNodes(_ => Lang.IfSome<SimpleBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<SimpleBgpRoute>>());
    return new SimpleSp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  public static SimpleSp<Unit> PathLength(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.IsNone<SimpleBgpRoute>(),
          Lang.IfSome(SimpleBgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.ForAllNodes(_ => Lang.IfSome<SimpleBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.ForAllNodes(_ =>
      Lang.Union(Lang.IsNone<SimpleBgpRoute>(), Lang.IfSome<SimpleBgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    return new SimpleSp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }
}

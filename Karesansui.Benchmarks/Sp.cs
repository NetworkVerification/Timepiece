using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

public class Sp<TS> : FatTree<Option<BatfishBgpRoute>, TS>
{
  public Sp(Topology topology, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    base(topology, destination,
      topology.ForAllEdges(_ => Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath)),
      Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
      Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()), Option.None<BatfishBgpRoute>(),
      annotations, stableProperties, safetyProperties, symbolics)
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
      distances.Select(p => (p.Key, Lang.Finally(p.Value, reachable)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties = topology.ForAllNodes(_ => reachable);
    // no safety property
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
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
          Lang.OrSome<BatfishBgpRoute>(b => Zen.And(b.LpEquals(0), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(BatfishBgpRouteExtensions.MaxLengthZeroLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.ForAllNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  public static Sp<Unit> PathLength(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.IsNone<BatfishBgpRoute>(),
          Lang.IfSome(BatfishBgpRouteExtensions.MaxLengthZeroLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      topology.ForAllNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.ForAllNodes(_ =>
      Lang.Union(Lang.IsNone<BatfishBgpRoute>(), Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    return new Sp<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }
}

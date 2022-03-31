using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

public class Sp<TS> : FatTree<TS>
{
  public Sp(uint numPods, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties, SymbolicValue<TS>[] symbolics) :
    base(numPods, destination, Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath),
      annotations, safetyProperties,
      symbolics)
  {
  }

  internal Sp(Topology topology, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties, SymbolicValue<TS>[] symbolics) :
    base(topology, destination,
      topology.ForAllEdges(_ => Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath)),
      annotations, safetyProperties, symbolics)
  {
  }
}

/// <summary>
/// Static factory class for Sp networks.
/// </summary>
public static class Sp
{
  public static Sp<Unit> Reachability(Topology topology, string destination)
  {
    var distances = topology.BreadthFirstSearch(destination);
    var reachable = Lang.IsSome<BatfishBgpRoute>();
    var annotations =
      distances.Select(p => (p.Key, Lang.Finally(p.Value, reachable)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var safetyProperties = topology.ForAllNodes(_ => reachable);
    return new Sp<Unit>(topology, destination, annotations, safetyProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>> MaxLength(BigInteger x) =>
    Lang.IfSome<BatfishBgpRoute>(b =>
      // Zen.And(b.GetAsPathLength() <= x, b.GetAsPathLength() >= new BigInteger(0)));
      Zen.And(b.GetAsPathLength() <= x, b.GetAsPathLength() >= new BigInteger(0), b.GetLp() == 0, b.GetOriginType() == new UInt2(0)));
      // Zen.And(b.GetAsPathLength() <= x, b.GetLp() == 0, b.GetOriginType() == new UInt2(0), b.GetMed() == 0));

  public static Sp<Unit> PathLength(Topology topology, string destination)
  {
    var distances = topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value, Lang.IsNone<BatfishBgpRoute>(), MaxLength(p.Value))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var safetyProperties = topology.ForAllNodes(_ => MaxLength(4));
    return new Sp<Unit>(topology, destination, annotations, safetyProperties, Array.Empty<SymbolicValue<Unit>>());
  }
}

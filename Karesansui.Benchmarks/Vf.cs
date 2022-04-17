using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

public class Vf<TS> : FatTree<Option<BatfishBgpRoute>, TS>
{
  public Vf(Topology topology, string destination, string tag,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    base(topology, destination, Transfer(topology, tag),
      Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
      Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()),
      Option.None<BatfishBgpRoute>(),
      annotations, stableProperties, safetyProperties, symbolics)
  {
  }

  private static Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>>
    Transfer(Topology topology, string tag)
  {
    return topology.ForAllEdges(e =>
    {
      var increment = Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath);
      var (src, snk) = e;
      if ((src.IsAggregation() && snk.IsEdge()) || src.IsCore())
      {
        // downward edge
        // add the tag on export if not already present
        var addTag = Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(b => b.AddCommunity(tag));
        return Lang.Compose(increment, addTag);
      }

      // upward edge
      // drop on import if down tag is present
      var import = Lang.Bind<BatfishBgpRoute, BatfishBgpRoute>(b => b.FilterCommunity(tag));
      return Lang.Compose(increment, import);
    });
  }
}

public static class Vf
{
  private const string DownTag = "down";

  public static Vf<Unit> ValleyFreeReachable(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var annotations =
      topology.ForAllNodes(n =>
        Lang.Until(distances[n], Lang.IsNone<BatfishBgpRoute>(),
          Lang.IfSome(distances[n] < 2
            // require that the safety property holds at time t, and that the LP equals the default, and the path length equals t
            ? b => Zen.And(Zen.Not(b.HasCommunity(DownTag)),
              BatfishBgpRouteExtensions.EqLengthDefaultLp(distances[n])(b))
            : BatfishBgpRouteExtensions.EqLengthDefaultLp(distances[n]))));
    var safetyProperties =
      topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var stableProperties =
      topology.ForAllNodes(_ => Lang.IsSome<BatfishBgpRoute>());
    return new Vf<Unit>(topology, destination, DownTag, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  public static Vf<Unit> ValleyFreePathLength(uint numPods, string destination)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var annotations =
      topology.ForAllNodes(n =>
        Lang.Until(distances[n], Lang.IsNone<BatfishBgpRoute>(),
          distances[n] < 2
            // require that the safety property holds at time t, and that the LP equals the default, and the path length equals t
            ? Lang.IfSome<BatfishBgpRoute>(b => Zen.And(Zen.Not(b.HasCommunity(DownTag)),
              BatfishBgpRouteExtensions.EqLengthDefaultLp(distances[n])(b)))
            : Lang.IfSome(BatfishBgpRouteExtensions.EqLengthDefaultLp(distances[n]))));
    var safetyProperties =
      topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var stableProperties =
      topology.ForAllNodes(_ => Lang.IfSome<BatfishBgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    return new Vf<Unit>(topology, destination, DownTag, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }
}

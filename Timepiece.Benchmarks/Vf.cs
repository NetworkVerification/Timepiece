using System.Numerics;
using MisterWolf;
using Timepiece.Networks;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Benchmarks;

public class Vf<TS> : Network<Option<BgpRoute>, TS>
{
  public Vf(Topology topology, Dictionary<string, Zen<Option<BgpRoute>>> initialValues, string tag,
    SymbolicValue<TS>[] symbolics) :
    base(topology, Transfer(topology, tag), Lang.Omap2<BgpRoute>(BgpRouteExtensions.Min),
      initialValues, symbolics)
  {
  }

  public Vf(Topology topology, string destination, string tag, SymbolicValue<TS>[] symbolics) :
    this(topology,
      topology.MapNodes(n => n == destination ? Option.Create<BgpRoute>(new BgpRoute()) : Option.Null<BgpRoute>()),
      tag, symbolics)
  {
  }

  private static Dictionary<(string, string), Func<Zen<Option<BgpRoute>>, Zen<Option<BgpRoute>>>> Transfer(
    Topology topology, string tag)
  {
    return topology.MapEdges(e =>
    {
      var increment = Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath);
      var (src, snk) = e;
      if ((src.IsAggregation() && snk.IsEdge()) || src.IsCore())
      {
        // downward edge
        // add the tag on export if not already present
        var addTag = Lang.Omap<BgpRoute, BgpRoute>(b => b.AddCommunity(tag));
        return Lang.Compose(increment, addTag);
      }

      // upward edge
      // drop on import if down tag is present
      var import = Lang.Bind<BgpRoute, BgpRoute>(b => b.FilterCommunity(tag));
      return Lang.Compose(increment, import);
    });
  }
}

public class AnnotatedVf<TS> : AnnotatedNetwork<Option<BgpRoute>, TS>
{
  public AnnotatedVf(Vf<TS> vf,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties) :
    base(vf, annotations, stableProperties, safetyProperties, new BigInteger(4))
  {
  }
}

public class InferVf : Infer<Option<BgpRoute>>
{
  public InferVf(Vf<Unit> vf, IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> afterInvariants) : base(vf, beforeInvariants,
    afterInvariants)
  {
  }
}

public static class Vf
{
  private const string DownTag = "down";

  private static Vf<Unit> ConcreteFatTreeVf(uint numPods, string destination)
  {
    return new Vf<Unit>(Topologies.FatTree(numPods), destination, DownTag, Array.Empty<SymbolicValue<Unit>>());
  }

  public static AnnotatedVf<Unit> ValleyFreeReachable(uint numPods, string destination, bool inferTimes)
  {
    var vf = ConcreteFatTreeVf(numPods, destination);
    var distances = vf.Topology.BreadthFirstSearch(destination);
    var afterConditions = vf.Topology.MapNodes(n =>
      Lang.IfSome(distances[n] < 2
        // require that the LP equals the default, and the path length equals the distance
        ? b => Zen.And(Zen.Not(b.HasCommunity(DownTag)),
          BgpRouteExtensions.EqLengthDefaultLp(distances[n])(b))
        : BgpRouteExtensions.EqLengthDefaultLp(distances[n])));

    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations;
    if (inferTimes)
    {
      var infer = new InferVf(vf, vf.Topology.MapNodes(_ => Lang.IsNone<BgpRoute>()), afterConditions)
      {
        MaxTime = 4
      };
      annotations = infer.InferAnnotations(InferenceStrategy.SymbolicEnumeration);
    }
    else
    {
      annotations =
        vf.Topology.MapNodes(n => Lang.Until(distances[n], Lang.IsNone<BgpRoute>(),
          afterConditions[n]));
    }

    var safetyProperties =
      vf.Topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties =
      vf.Topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    return new AnnotatedVf<Unit>(vf, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedVf<Unit> ValleyFreePathLength(uint numPods, string destination)
  {
    var vf = ConcreteFatTreeVf(numPods, destination);
    var distances = vf.Topology.BreadthFirstSearch(destination);
    var annotations =
      vf.Topology.MapNodes(n =>
        Lang.Until(distances[n], Lang.IsNone<BgpRoute>(),
          distances[n] < 2
            // require that the safety property holds at time t, and that the LP equals the default, and the path length equals t
            ? Lang.IfSome<BgpRoute>(b => Zen.And(Zen.Not(b.HasCommunity(DownTag)),
              BgpRouteExtensions.EqLengthDefaultLp(distances[n])(b)))
            : Lang.IfSome(BgpRouteExtensions.EqLengthDefaultLp(distances[n]))));
    var safetyProperties =
      vf.Topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties =
      vf.Topology.MapNodes(_ => Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    return new AnnotatedVf<Unit>(vf, annotations, stableProperties, safetyProperties);
  }


  public static AnnotatedVf<Pair<string, int>> AllPairsValleyFreeReachable(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    var initialValues =
      topology.MapNodes(n => Option.Create<BgpRoute>(new BgpRoute())
        .Where(_ => dest.Equals(topology, n)));
    var symbolics = new SymbolicValue<Pair<string, int>>[] {dest};
    var vf = new Vf<Pair<string, int>>(topology, initialValues, DownTag, symbolics);
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance, Lang.IsNone<BgpRoute>(),
          Lang.IfSome<BgpRoute>(b =>
            Zen.If(distance < new BigInteger(2),
              // require that the safety property holds at time t,
              // and that the LP equals the default, and the path length equals t
              Zen.And(Zen.Not(b.HasCommunity(DownTag)),
                BgpRouteExtensions.EqLengthDefaultLp(distance)(b)),
              BgpRouteExtensions.EqLengthDefaultLp(distance)(b))));
      });
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    return new AnnotatedVf<Pair<string, int>>(vf, annotations, stableProperties, safetyProperties);
  }
}

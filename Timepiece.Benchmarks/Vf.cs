using System.Numerics;
using MisterWolf;
using Timepiece.Networks;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Benchmarks;

public partial class Vf : Network<Option<BgpRoute>, string>
{
  public Vf(Digraph<string> digraph, Dictionary<string, Zen<Option<BgpRoute>>> initialValues, string tag,
    ISymbolic[] symbolics) :
    base(digraph, Transfer(digraph, tag), Lang.Omap2<BgpRoute>(BgpRouteExtensions.Min),
      initialValues, symbolics)
  {
  }

  public Vf(Digraph<string> digraph, string destination, string tag, ISymbolic[] symbolics) :
    this(digraph,
      digraph.MapNodes(n => n.Equals(destination) ? Option.Create<BgpRoute>(new BgpRoute()) : Option.Null<BgpRoute>()),
      tag, symbolics)
  {
  }

  private static Dictionary<(string, string), Func<Zen<Option<BgpRoute>>, Zen<Option<BgpRoute>>>> Transfer(
    Digraph<string> digraph, string tag)
  {
    return digraph.MapEdges(e =>
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

public class AnnotatedVf : AnnotatedNetwork<Option<BgpRoute>, string>
{
  public AnnotatedVf(Vf vf,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties) :
    base(vf, annotations, stableProperties, safetyProperties, new BigInteger(4))
  {
  }

  public AnnotatedVf(Vf vf,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> monolithicProperties) : base(vf, annotations,
    modularProperties, monolithicProperties)
  {
  }
}

public class InferVf : Infer<Option<BgpRoute>, string>
{
  public InferVf(Vf vf, IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> afterInvariants) : base(vf, beforeInvariants,
    afterInvariants)
  {
  }
}

public partial class Vf
{
  private const string DownTag = "down";

  private static Vf ConcreteFatTreeVf(uint numPods, string destination)
  {
    return new Vf(Topologies.FatTree(numPods), destination, DownTag, Array.Empty<ISymbolic>());
  }

  public static AnnotatedVf ValleyFreeReachable(uint numPods, string destination, bool inferTimes)
  {
    var vf = ConcreteFatTreeVf(numPods, destination);
    var distances = vf.Digraph.BreadthFirstSearch(destination);
    var afterConditions = vf.Digraph.MapNodes(n =>
      Lang.IfSome(distances[n] < 2
        // require that the LP equals the default, and the path length equals the distance
        ? b => Zen.And(Zen.Not(b.HasCommunity(DownTag)),
          BgpRouteExtensions.EqLengthDefaultLp(distances[n])(b))
        : BgpRouteExtensions.EqLengthDefaultLp(distances[n])));

    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations;
    if (inferTimes)
    {
      var infer = new InferVf(vf, vf.Digraph.MapNodes(_ => Lang.IsNone<BgpRoute>()), afterConditions)
      {
        MaxTime = 4
      };
      annotations = infer.InferAnnotationsWithStats(InferenceStrategy.SymbolicEnumeration);
    }
    else
    {
      annotations =
        vf.Digraph.MapNodes(n => Lang.Until(distances[n], Lang.IsNone<BgpRoute>(),
          afterConditions[n]));
    }

    var safetyProperties =
      vf.Digraph.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties =
      vf.Digraph.MapNodes(_ => Lang.IsSome<BgpRoute>());
    return new AnnotatedVf(vf, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedVf ValleyFreeReachableSymbolicTimes(uint numPods, string destination)
  {
    var times = FatTreeSymbolicTimes.AscendingSymbolicTimes(5).ToArray();
    var lastTime = times[^1].Value;
    var g = Topologies.LabelledFatTree(numPods);
    var monolithicProperties = g.MapNodes(_ => Lang.IsSome<BgpRoute>());
    var modularProperties = g.MapNodes(n => Lang.Finally(lastTime, monolithicProperties[n]));
    var annotations = g.MapNodes(n =>
    {
      var dist = n.DistanceFromDestinationEdge(g.L(n), destination, g.L(destination));
      var time = times[dist].Value;
      var maxPathLength = new BigInteger(dist);
      // at all times, either a node has no route, or it has a route equal to the initial route modulo path length
      var safety =
        Lang.Globally(Lang.OrSome(n.IsCore()
          ? Lang.Intersect<BgpRoute>(EqualsInitialRouteModLength, b => Zen.Not(b.HasCommunity(DownTag)))
          : EqualsInitialRouteModLength));
      var eventually =
        Lang.Finally(time,
          Lang.IfSome(n == destination || (n.IsAggregation() && g.L(n) == g.L(destination))
            // require that the LP equals the default, and the path length equals the distance
            ? b => Zen.And(Zen.Not(b.HasCommunity(DownTag)), BgpRouteExtensions.MaxLengthDefaultLp(maxPathLength)(b))
            : BgpRouteExtensions.MaxLengthDefaultLp(maxPathLength)));
      return Lang.Intersect(safety, eventually);
    });
    var vf = new Vf(g, destination, DownTag, times.Cast<ISymbolic>().ToArray());
    return new AnnotatedVf(vf, annotations, modularProperties, monolithicProperties);
  }

  private static Zen<bool> EqualsInitialRouteModLength(Zen<BgpRoute> r)
  {
    var initialRoute = new BgpRoute();
    return Zen.And(r.LpEquals(initialRoute.Lp),
      r.GetAsPathLength() >= BigInteger.Zero,
      r.GetOriginType() == initialRoute.OriginType,
      r.GetMed() == initialRoute.Med);
  }

  public static AnnotatedVf ValleyFreePathLength(uint numPods, string destination)
  {
    var vf = ConcreteFatTreeVf(numPods, destination);
    var distances = vf.Digraph.BreadthFirstSearch(destination);
    var annotations =
      vf.Digraph.MapNodes(n =>
        Lang.Until(distances[n], Lang.IsNone<BgpRoute>(),
          distances[n] < 2
            // require that the safety property holds at time t, and that the LP equals the default, and the path length equals t
            ? Lang.IfSome<BgpRoute>(b => Zen.And(Zen.Not(b.HasCommunity(DownTag)),
              BgpRouteExtensions.EqLengthDefaultLp(distances[n])(b)))
            : Lang.IfSome(BgpRouteExtensions.EqLengthDefaultLp(distances[n]))));
    var safetyProperties =
      vf.Digraph.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties =
      vf.Digraph.MapNodes(_ => Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    return new AnnotatedVf(vf, annotations, stableProperties, safetyProperties);
  }


  public static AnnotatedVf AllPairsValleyFreeReachable(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    var initialValues =
      topology.MapNodes(n => Option.Create<BgpRoute>(new BgpRoute())
        .Where(_ => dest.Equals(topology, n)));
    ISymbolic[] symbolics = {dest};
    var vf = new Vf(topology, initialValues, DownTag, symbolics);
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
    return new AnnotatedVf(vf, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedVf AllPairsValleyFreeReachableSymbolicTimes(uint numPods)
  {
    var times = FatTreeSymbolicTimes.AscendingSymbolicTimes(5);
    var g = Topologies.LabelledFatTree(numPods);
    var lastTime = times[^1].Value;
    var dest = new SymbolicDestination(g);
    var initialValues =
      g.MapNodes(n => Option.Create<BgpRoute>(new BgpRoute())
        .Where(_ => dest.Equals(g, n)));
    var monolithicProperties = g.MapNodes(_ => Lang.IsSome<BgpRoute>());
    var modularProperties = g.MapNodes(n => Lang.Finally(lastTime, monolithicProperties[n]));
    var symbolics = new[] {dest}.Concat<ISymbolic>(times).ToArray();
    var vf = new Vf(g, initialValues, DownTag, symbolics);
    // the time at which the core nodes converge
    var coreTime = times[2].Value;
    var annotations = g.MapNodes(n =>
    {
      var dist = dest.SymbolicDistanceCases(n, g.L(n), times.Select(t => t.Value).ToList());
      // at all times, either a node has no route, or it has a route equal to the initial route modulo path length
      var safety =
        Lang.Globally(Lang.OrSome<BgpRoute>(Lang.Intersect<BgpRoute>(EqualsInitialRouteModLength)));
      var eventually =
        Lang.Finally(dist,
          Lang.IfSome<BgpRoute>(b =>
            Zen.And(
              // core and close-to-destination nodes should not be tagged down
              Zen.Implies(Zen.Or(n.IsCore(), dist < coreTime), Zen.Not(b.HasCommunity(DownTag))),
              // require that the LP equals the default, and the path length equals the distance
              BgpRouteExtensions.MaxLengthDefaultLp(dist)(b))));
      return Lang.Intersect(safety, eventually);
    });
    return new AnnotatedVf(vf, annotations, modularProperties, monolithicProperties);
  }
}

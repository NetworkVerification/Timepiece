using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

// a route which is tagged as internal (false) or external (true)
using TaggedRoute = Pair<Option<BgpRoute>, bool>;

public class Hijack<TS> : AnnotatedNetwork<TaggedRoute, TS>
{
  public Hijack(Topology topology, Dictionary<string, Zen<TaggedRoute>> initialValues, string hijacker,
    Zen<uint> destinationPrefix,
    Dictionary<string, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics)
    : base(topology, Transfer(topology, hijacker, destinationPrefix),
      Merge(destinationPrefix),
      initialValues,
      annotations,
      stableProperties, safetyProperties, new BigInteger(4),
      symbolics)
  {
  }

  public Hijack(Topology topology, string destination, string hijacker,
    Zen<Option<BgpRoute>> hijackRoute,
    Zen<uint> destinationPrefix,
    Dictionary<string, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics)
    : this(topology,
      topology.MapNodes(n => n == destination
        ? Pair.Create(
          Option.Create(BgpRouteExtensions.ToDestination(destinationPrefix)),
          Zen.False())
        : n == hijacker
          ? Pair.Create(hijackRoute, Zen.True())
          : Pair.Create<Option<BgpRoute>, bool>(Option.None<BgpRoute>(), Zen.False())),
      hijacker, destinationPrefix, annotations, stableProperties, safetyProperties, symbolics)
  {
  }

  private static Func<Zen<TaggedRoute>, Zen<TaggedRoute>, Zen<TaggedRoute>> Merge(Zen<uint> destinationPrefix)
  {
    return Lang.MergeBy<TaggedRoute, Option<BgpRoute>>(
      Lang.Omap2<BgpRoute>((b1, b2) => b1.MinPrefix(b2, destinationPrefix)),
      t => t.Item1());
  }

  /// <summary>
  ///   Define the transfer function to filter all routes claiming to be from the
  ///   destination prefix sent from the hijacker.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="hijacker"></param>
  /// <param name="destinationPrefix"></param>
  /// <returns></returns>
  private static Dictionary<(string, string), Func<Zen<TaggedRoute>, Zen<TaggedRoute>>> Transfer(Topology topology,
    string hijacker, Zen<uint> destinationPrefix)
  {
    return topology.MapEdges(e =>
      Lang.Product(
        Lang.Test(
          Lang.IfSome<BgpRoute>(b => Zen.And(b.GetDestination() == destinationPrefix, e.Item1 == hijacker)),
          Lang.Const(Option.None<BgpRoute>()),
          Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath)),
        Lang.Identity<bool>()));
  }
}

public static class Hijack
{
  private const string HijackNode = "hijacker";

  public static Hijack<Pair<Option<BgpRoute>, uint>> HijackFiltered(uint numPods, string destination)
  {
    var topology = HijackTopology(HijackNode, Topologies.FatTree(numPods));
    var hijackAndPrefix = HijackRouteAndPrefix();
    var hijackRoute = hijackAndPrefix.Value.Item1();
    var destinationPrefix = hijackAndPrefix.Value.Item2();
    var distances = topology.BreadthFirstSearch(destination);
    var annotations =
      distances.Select(p => (p.Key,
          // hijacker annotation is just true
          p.Key == HijackNode
            ? Lang.Globally(Lang.True<TaggedRoute>())
            : Lang.Intersect(
              Lang.Globally<TaggedRoute>(r => DestinationRouteIsInternal(destinationPrefix, r)),
              Lang.Finally(p.Value,
                MapInternal(
                  r => HasDestinationRoute(destinationPrefix, r))))))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var stableProperties =
      topology.MapNodes(n =>
        n == HijackNode ? Lang.True<TaggedRoute>() : MapInternal(r => HasDestinationRoute(destinationPrefix, r)));
    var safetyProperties = topology.MapNodes(_ => Lang.True<TaggedRoute>());
    return new Hijack<Pair<Option<BgpRoute>, uint>>(topology, destination, HijackNode, hijackRoute,
      destinationPrefix,
      annotations, stableProperties, safetyProperties, new[] {hijackAndPrefix});
  }

  private static Func<Zen<TaggedRoute>, Zen<bool>> MapInternal(Func<Zen<Option<BgpRoute>>, Zen<bool>> f)
  {
    return Lang.Both<Option<BgpRoute>, bool>(f, Zen.Not);
  }

  private static Zen<bool> HasDestinationRoute(Zen<uint> prefix, Zen<Option<BgpRoute>> o)
  {
    return o.Where(b => b.DestinationIs(prefix)).IsSome();
  }

  private static Zen<bool> DestinationRouteIsInternal(Zen<uint> prefix, Zen<TaggedRoute> r)
  {
    return Zen.Implies(HasDestinationRoute(prefix, r.Item1()), Zen.Not(r.Item2()));
  }

  public static Hijack<Pair<Option<BgpRoute>, uint, string, int>> AllPairsHijackFiltered(uint numPods)
  {
    var topology = HijackTopology(HijackNode, Topologies.LabelledFatTree(numPods), -1);
    var symbolicData = new SymbolicValue<Pair<Option<BgpRoute>, uint, string, int>>(
      "hijackRouteAndPrefixAndNodeAndPod",
      p => topology.ExistsNode(n =>
        Zen.And(n.IsEdge(), p.Item3() == n, p.Item4() == topology.L(n))));
    var hijackRoute = symbolicData.Value.Item1();
    var destinationPrefix = symbolicData.Value.Item2();
    var destination = symbolicData.Value.Item3();
    var destinationPod = symbolicData.Value.Item4();
    var annotations =
      topology.MapNodes(n =>
        // hijacker annotation is just true
        n == HijackNode
          ? Lang.Globally(Lang.True<TaggedRoute>())
          : Lang.Intersect(
            Lang.Globally<TaggedRoute>(r => DestinationRouteIsInternal(destinationPrefix, r)),
            Lang.Finally(Zen.If(destination == n, BigInteger.Zero,
                Zen.If(Zen.And(n.IsAggregation(), destinationPod == topology.L(n)), new BigInteger(1),
                  Zen.If(Zen.And(n.IsAggregation(), destinationPod != topology.L(n)), new BigInteger(3),
                    Zen.If<BigInteger>(Zen.And(n.IsEdge(), destinationPod != topology.L(n)), new BigInteger(4),
                      new BigInteger(2))))),
              MapInternal(
                r => HasDestinationRoute(destinationPrefix, r)))));
    var stableProperties =
      topology.MapNodes(n =>
        n == HijackNode ? Lang.True<TaggedRoute>() : MapInternal(r => HasDestinationRoute(destinationPrefix, r)));
    var safetyProperties = topology.MapNodes(_ => Lang.True<TaggedRoute>());
    var initialValues = topology.MapNodes(n =>
      Pair.Create<Option<BgpRoute>, bool>(
        n == HijackNode
          ? hijackRoute
          : Option.Create(BgpRouteExtensions.ToDestination(destinationPrefix)).Where(_ => n == destination),
        n == HijackNode));
    return new Hijack<Pair<Option<BgpRoute>, uint, string, int>>(topology, initialValues, HijackNode,
      destinationPrefix, annotations, stableProperties, safetyProperties, new[] {symbolicData});
  }

  // old hijack; do we still need this?
  public static Hijack<Pair<Option<BgpRoute>, uint>> HijackFilteredOld(uint numPods, string destination)
  {
    var topology = HijackTopology(HijackNode, Topologies.FatTree(numPods));
    var hijackAndPrefix = HijackRouteAndPrefix();
    var hijackRoute = hijackAndPrefix.Value.Item1();
    var destinationPrefix = hijackAndPrefix.Value.Item2();
    var distances = topology.BreadthFirstSearch(destination);
    var annotations =
      distances
        .Select<KeyValuePair<string, BigInteger>, (string Key, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>)>(p =>
          (p.Key,
            // hijacker annotation is just true
            p.Key == HijackNode
              ? Lang.Globally(Lang.True<TaggedRoute>())
              : Lang.Until<TaggedRoute>(p.Value, r => DestinationRouteIsInternal(destinationPrefix, r),
                route => Zen.And(DestinationRouteIsInternal(destinationPrefix, route), route.Item1().IsSome()))))
        .ToDictionary(p => p.Item1, p => p.Item2);
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties =
      topology.MapNodes(n =>
        n == HijackNode
          ? Lang.True<TaggedRoute>()
          : Lang.First<Option<BgpRoute>, bool>(Lang.IsSome<BgpRoute>()));
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties =
      topology.MapNodes(n =>
        n == HijackNode
          ? Lang.True<TaggedRoute>()
          : p => Zen.Implies(HasDestinationRoute(destinationPrefix, p.Item1()), Zen.Not(p.Item2())));
    return new Hijack<Pair<Option<BgpRoute>, uint>>(topology, destination, HijackNode, hijackRoute,
      destinationPrefix,
      annotations, stableProperties, safetyProperties, new[] {hijackAndPrefix});
  }

  public static SymbolicValue<Pair<Option<BgpRoute>, uint>> HijackRouteAndPrefix()
  {
    return new("hijackAndPrefix");
  }

  /// <summary>
  ///   Add a hijacker node to the topology, connected to all of the core nodes.
  /// </summary>
  /// <param name="hijacker"></param>
  /// <param name="topology"></param>
  /// <param name="hijackerLabel"></param>
  /// <returns></returns>
  private static LabelledTopology<T> HijackTopology<T>(string hijacker, LabelledTopology<T> topology, T hijackerLabel)
  {
    var withHijacker = HijackAdjList(hijacker, topology);
    var labels = topology.Labels;
    labels[hijacker] = hijackerLabel;

    return new LabelledTopology<T>(withHijacker, labels);
  }

  private static Topology HijackTopology(string hijacker, Topology topology)
  {
    return new Topology(HijackAdjList(hijacker, topology));
  }

  private static Dictionary<string, List<string>> HijackAdjList(string hijacker,
    Topology topology)
  {
    var withHijacker = topology.Neighbors;
    withHijacker[hijacker] = topology.Nodes.Where(n => n.IsCore()).ToList();
    foreach (var node in withHijacker[hijacker]) withHijacker[node].Add(hijacker);

    return withHijacker;
  }
}

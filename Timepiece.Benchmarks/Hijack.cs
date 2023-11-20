using System.Collections.Immutable;
using System.Numerics;
using MisterWolf;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

// a route which is tagged as internal (false) or external (true)
using TaggedRoute = Pair<Option<BgpRoute>, bool>;

public class Hijack<NodeType> : Network<TaggedRoute, NodeType>
  where NodeType : IEquatable<NodeType>
{
  public Hijack(Digraph<NodeType> digraph, Dictionary<NodeType, Zen<TaggedRoute>> initialValues, NodeType hijacker,
    Zen<uint> destinationPrefix, ISymbolic[] symbolics)
    : base(digraph, Transfer(digraph, hijacker, destinationPrefix), Merge(destinationPrefix), initialValues,
      symbolics)
  {
  }

  public Hijack(Digraph<NodeType> digraph, NodeType destination, NodeType hijacker,
    Zen<Option<BgpRoute>> hijackRoute,
    Zen<uint> destinationPrefix,
    ISymbolic[] symbolics)
    : this(digraph,
      digraph.MapNodes(n => n.Equals(destination)
        ? Pair.Create(
          Option.Create(BgpRouteExtensions.ToDestination(destinationPrefix)),
          Zen.False())
        : n.Equals(hijacker)
          ? Pair.Create(hijackRoute, Zen.True())
          : Pair.Create<Option<BgpRoute>, bool>(Option.None<BgpRoute>(), Zen.False())),
      hijacker, destinationPrefix, symbolics)
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
  /// <param name="digraph"></param>
  /// <param name="hijacker"></param>
  /// <param name="destinationPrefix"></param>
  /// <returns></returns>
  private static Dictionary<(NodeType, NodeType), Func<Zen<TaggedRoute>, Zen<TaggedRoute>>> Transfer(
    Digraph<NodeType> digraph,
    NodeType hijacker, Zen<uint> destinationPrefix)
  {
    return digraph.MapEdges(e =>
      Lang.Product(
        Lang.Test(
          Lang.IfSome<BgpRoute>(b => Zen.And(b.GetDestination() == destinationPrefix, e.Item1.Equals(hijacker))),
          Lang.Const(Option.None<BgpRoute>()),
          Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath)),
        Lang.Identity<bool>()));
  }
}

public class AnnotatedHijack<NodeType> : AnnotatedNetwork<TaggedRoute, NodeType>
  where NodeType : IEquatable<NodeType>
{
  public AnnotatedHijack(Network<TaggedRoute, NodeType> net,
    Dictionary<NodeType, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties)
    : base(net, annotations, stableProperties, safetyProperties, new BigInteger(4))
  {
  }

  public AnnotatedHijack(Network<TaggedRoute, NodeType> net,
    Dictionary<NodeType, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<NodeType, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> monolithicProperties) :
    base(net, annotations, modularProperties, monolithicProperties)
  {
  }
}

public class InferHijack<NodeType> : Infer<TaggedRoute, NodeType>
  where NodeType : IEquatable<NodeType>
{
  public InferHijack(Network<TaggedRoute, NodeType> hijack,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> afterInvariants) : base(hijack, beforeInvariants,
    afterInvariants)
  {
  }
}

public static class Hijack
{
  private const string HijackNode = "hijacker";

  public static AnnotatedHijack<string> HijackFiltered(uint numPods, string destination,
    bool inferTimes)
  {
    var topology = HijackTopology(HijackNode, Topologies.FatTree(numPods));
    var hijackRoute = new SymbolicValue<Option<BgpRoute>>("hijackRoute");
    var destinationPrefix = new SymbolicValue<uint>("destinationPrefix");
    var hijack = new Hijack<string>(topology, destination, HijackNode, hijackRoute.Value,
      destinationPrefix.Value, new ISymbolic[] {hijackRoute, destinationPrefix});
    var stableProperties =
      topology.MapNodes(n =>
        n == HijackNode ? Lang.True<TaggedRoute>() : MapInternal(r => HasDestinationRoute(destinationPrefix, r)));
    var safetyProperties = topology.MapNodes(_ => Lang.True<TaggedRoute>());
    Dictionary<string, Func<Zen<Pair<Option<BgpRoute>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations;
    if (inferTimes)
    {
      var beforeInvariants = hijack.Digraph.MapNodes(n =>
      {
        return n == HijackNode ? Lang.True<TaggedRoute>() : r => DestinationRouteIsInternal(destinationPrefix, r);
      });
      var afterInvariants = hijack.Digraph.MapNodes(n => n == HijackNode
        ? Lang.True<TaggedRoute>()
        : Lang.Intersect(r => DestinationRouteIsInternal(destinationPrefix, r),
          MapInternal(r => HasDestinationRoute(destinationPrefix, r))));
      var infer = new InferHijack<string>(hijack, beforeInvariants, afterInvariants)
      {
        MaxTime = 4
      };
      annotations = infer.InferAnnotationsWithStats(InferenceStrategy.SymbolicEnumeration);
    }
    else
    {
      var distances = topology.BreadthFirstSearch(destination);
      annotations =
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
    }

    return new AnnotatedHijack<string>(hijack, annotations, stableProperties,
      safetyProperties);
  }

  public static AnnotatedHijack<string> HijackFilteredSymbolicTimes(uint numPods, string destination)
  {
    var topology = HijackTopology(HijackNode, Topologies.LabelledFatTree(numPods), -1);
    var hijackRoute = new SymbolicValue<Option<BgpRoute>>("hijackRoute");
    var destinationPrefix = new SymbolicValue<uint>("destinationPrefix");
    var times = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = times[^1].Value;
    var hijack = new Hijack<string>(topology, destination, HijackNode, hijackRoute.Value,
      destinationPrefix.Value, new ISymbolic[] {hijackRoute, destinationPrefix}.Concat(times).ToArray());
    var afterPredicateInternal = MapInternal(r => HasDestinationRoute(destinationPrefix, r));
    var monolithicProperties = topology.MapNodes(n =>
      n == HijackNode ? Lang.True<TaggedRoute>() : afterPredicateInternal);
    var modularProperties = topology.MapNodes(n =>
      n == HijackNode ? Lang.Globally(Lang.True<TaggedRoute>()) : Lang.Finally(lastTime, monolithicProperties[n]));
    var eventuallyAnnotations = FatTreeSymbolicTimes.FinallyAnnotations(topology, destination, afterPredicateInternal,
      times.Select(t => t.Value).ToList());
    Dictionary<string, Func<Zen<Pair<Option<BgpRoute>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations =
      topology.MapNodes(n =>
      {
        if (n == HijackNode)
          return Lang.Globally(Lang.True<TaggedRoute>());

        var safety = Lang.Globally<TaggedRoute>(r => DestinationRouteIsInternal(destinationPrefix, r));
        return Lang.Intersect(safety, eventuallyAnnotations[n]);
      });

    return new AnnotatedHijack<string>(hijack, annotations, modularProperties, monolithicProperties);
  }

  private static Func<Zen<TaggedRoute>, Zen<bool>> MapInternal(Func<Zen<Option<BgpRoute>>, Zen<bool>> f)
  {
    return Lang.Both<Option<BgpRoute>, bool>(f, Zen.Not);
  }

  private static Zen<bool> HasDestinationRoute(SymbolicValue<uint> prefix, Zen<Option<BgpRoute>> o)
  {
    return o.Where(b => b.DestinationIs(prefix.Value)).IsSome();
  }

  private static Zen<bool> DestinationRouteIsInternal(SymbolicValue<uint> prefix, Zen<TaggedRoute> r)
  {
    return Zen.Implies(HasDestinationRoute(prefix, r.Item1()), Zen.Not(r.Item2()));
  }

  public static AnnotatedHijack<string> AllPairsHijackFiltered(uint numPods)
  {
    var topology = HijackTopology(HijackNode, Topologies.LabelledFatTree(numPods), -1);
    var hijackRoute = new SymbolicValue<Option<BgpRoute>>("hijackRoute");
    var destinationPrefix = new SymbolicValue<uint>("destinationPrefix");
    var destination = new SymbolicDestination(topology);
    var initialValues = topology.MapNodes(n =>
      Pair.Create<Option<BgpRoute>, bool>(
        n == HijackNode
          ? hijackRoute.Value
          : Option.Create(BgpRouteExtensions.ToDestination(destinationPrefix.Value))
            .Where(_ => destination.EqualsDigraph(topology, n)),
        n == HijackNode));
    var hijack = new Hijack<string>(topology, initialValues, HijackNode,
      destinationPrefix.Value, new ISymbolic[] {hijackRoute, destinationPrefix, destination});
    var annotations =
      topology.MapNodes(n =>
        // hijacker annotation is just true
        n == HijackNode
          ? Lang.Globally(Lang.True<TaggedRoute>())
          : Lang.Intersect(
            Lang.Globally<TaggedRoute>(r => DestinationRouteIsInternal(destinationPrefix, r)),
            Lang.Finally(destination.SymbolicDistance(n, topology.L(n)),
              MapInternal(
                r => HasDestinationRoute(destinationPrefix, r)))));
    var stableProperties =
      topology.MapNodes(n =>
        n == HijackNode ? Lang.True<TaggedRoute>() : MapInternal(r => HasDestinationRoute(destinationPrefix, r)));
    var safetyProperties = topology.MapNodes(_ => Lang.True<TaggedRoute>());
    return new AnnotatedHijack<string>(hijack, annotations, stableProperties,
      safetyProperties);
  }

  public static AnnotatedHijack<string> AllPairsHijackFilteredSymbolicTimes(uint numPods)
  {
    var topology = HijackTopology(HijackNode, Topologies.LabelledFatTree(numPods), -1);
    var hijackRoute = new SymbolicValue<Option<BgpRoute>>("hijackRoute");
    var destinationPrefix = new SymbolicValue<uint>("destinationPrefix");
    var destination = new SymbolicDestination(topology);
    var initialRoutes = topology.MapNodes(n =>
      Pair.Create<Option<BgpRoute>, bool>(
        n == HijackNode
          ? hijackRoute.Value
          : Option.Create(BgpRouteExtensions.ToDestination(destinationPrefix.Value))
            .Where(_ => destination.EqualsDigraph(topology, n)),
        n == HijackNode));
    var times = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = times[^1].Value;
    var hijack = new Hijack<string>(topology, initialRoutes, HijackNode,
      destinationPrefix.Value, new ISymbolic[] {hijackRoute, destinationPrefix, destination}.Concat(times).ToArray());
    var afterPredicateInternal = MapInternal(r => HasDestinationRoute(destinationPrefix, r));
    var monolithicProperties = topology.MapNodes(n =>
      n == HijackNode ? Lang.True<TaggedRoute>() : afterPredicateInternal);
    var modularProperties = topology.MapNodes(n =>
      n == HijackNode ? Lang.Globally(Lang.True<TaggedRoute>()) : Lang.Finally(lastTime, monolithicProperties[n]));
    var eventuallyAnnotations = FatTreeSymbolicTimes.FinallyAnnotations(topology, destination, afterPredicateInternal,
      times.Select(t => t.Value).ToList());
    Dictionary<string, Func<Zen<Pair<Option<BgpRoute>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations =
      topology.MapNodes(n =>
      {
        if (n == HijackNode)
          return Lang.Globally(Lang.True<TaggedRoute>());

        var safety = Lang.Globally<TaggedRoute>(r => DestinationRouteIsInternal(destinationPrefix, r));
        return Lang.Intersect(safety, eventuallyAnnotations[n]);
      });

    return new AnnotatedHijack<string>(hijack, annotations, modularProperties, monolithicProperties);
  }

  /// <summary>
  ///   Add a hijacker node to the topology, connected to all of the core nodes.
  /// </summary>
  /// <param name="hijacker"></param>
  /// <param name="digraph"></param>
  /// <param name="hijackerLabel"></param>
  /// <returns></returns>
  private static NodeLabelledDigraph<string, T> HijackTopology<T>(string hijacker,
    NodeLabelledDigraph<string, T> digraph,
    T hijackerLabel)
  {
    var withHijacker = HijackAdjList(hijacker, digraph);
    var labels = digraph.Labels;
    labels[hijacker] = hijackerLabel;

    return new NodeLabelledDigraph<string, T>(withHijacker, labels);
  }

  private static Digraph<string> HijackTopology(string hijacker, Digraph<string> digraph)
  {
    return new Digraph<string>(HijackAdjList(hijacker, digraph));
  }

  private static IDictionary<string, ImmutableSortedSet<string>> HijackAdjList(string hijacker,
    Digraph<string> digraph)
  {
    var withHijacker = digraph.Neighbors;
    withHijacker[hijacker] = digraph.Nodes.Where(n => n.IsCore()).ToImmutableSortedSet();
    foreach (var node in withHijacker[hijacker])
      withHijacker[node] = withHijacker[node].Add(hijacker);

    return withHijacker;
  }
}

using System.Collections.Immutable;
using System.Numerics;
using MisterWolf;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

// a route which is tagged as internal (false) or external (true)
using TaggedRoute = Pair<Option<BgpRoute>, bool>;

public class Hijack<NodeType, SymbolicType> : Network<TaggedRoute, NodeType, SymbolicType>
  where NodeType : IEquatable<NodeType>
{
  public Hijack(Digraph<NodeType> digraph, Dictionary<NodeType, Zen<TaggedRoute>> initialValues, NodeType hijacker,
    Zen<uint> destinationPrefix, SymbolicValue<SymbolicType>[] symbolics)
    : base(digraph, Transfer(digraph, hijacker, destinationPrefix), Merge(destinationPrefix), initialValues,
      symbolics)
  {
  }

  public Hijack(Digraph<NodeType> digraph, NodeType destination, NodeType hijacker,
    Zen<Option<BgpRoute>> hijackRoute,
    Zen<uint> destinationPrefix,
    SymbolicValue<SymbolicType>[] symbolics)
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

public class AnnotatedHijack<NodeType, SymbolicType> : AnnotatedNetwork<TaggedRoute, NodeType, SymbolicType>
  where NodeType : IEquatable<NodeType>
{
  public AnnotatedHijack(Network<TaggedRoute, NodeType, SymbolicType> net,
    Dictionary<NodeType, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties)
    : base(net, annotations, stableProperties, safetyProperties, new BigInteger(4))
  {
  }
}

public class InferHijack<NodeType> : Infer<TaggedRoute, NodeType, Pair<Option<BgpRoute>, uint>>
  where NodeType : IEquatable<NodeType>
{
  public InferHijack(Network<TaggedRoute, NodeType, Pair<Option<BgpRoute>, uint>> hijack,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<NodeType, Func<Zen<TaggedRoute>, Zen<bool>>> afterInvariants) : base(hijack, beforeInvariants,
    afterInvariants)
  {
  }
}

public static class Hijack
{
  private const string HijackNode = "hijacker";

  public static AnnotatedHijack<string, Pair<Option<BgpRoute>, uint>> HijackFiltered(uint numPods, string destination,
    bool inferTimes)
  {
    var topology = HijackTopology(HijackNode, Topologies.FatTree(numPods));
    var hijackAndPrefix = HijackRouteAndPrefix();
    var hijackRoute = hijackAndPrefix.Value.Item1();
    var destinationPrefix = hijackAndPrefix.Value.Item2();
    var hijack = new Hijack<string, Pair<Option<BgpRoute>, uint>>(topology, destination, HijackNode, hijackRoute,
      destinationPrefix, new[] {hijackAndPrefix});
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

    return new AnnotatedHijack<string, Pair<Option<BgpRoute>, uint>>(hijack, annotations, stableProperties,
      safetyProperties);
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

  public static AnnotatedHijack<string, Pair<Option<BgpRoute>, uint, string, int>> AllPairsHijackFiltered(uint numPods)
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
    var initialValues = topology.MapNodes(n =>
      Pair.Create<Option<BgpRoute>, bool>(
        n == HijackNode
          ? hijackRoute
          : Option.Create(BgpRouteExtensions.ToDestination(destinationPrefix)).Where(_ => n == destination),
        n == HijackNode));
    var hijack = new Hijack<string, Pair<Option<BgpRoute>, uint, string, int>>(topology, initialValues, HijackNode,
      destinationPrefix, new[] {symbolicData});
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
    return new AnnotatedHijack<string, Pair<Option<BgpRoute>, uint, string, int>>(hijack, annotations, stableProperties,
      safetyProperties);
  }

  private static SymbolicValue<Pair<Option<BgpRoute>, uint>> HijackRouteAndPrefix() => new("hijackAndPrefix");

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

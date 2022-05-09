using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

// a route which is tagged as internal (false) or external (true)
using TaggedRoute = Pair<Option<BatfishBgpRoute>, bool>;

public class Hijack : Network<TaggedRoute, Pair<Option<BatfishBgpRoute>, uint>>
{
  // TODO: should this not be static?
  private static SymbolicValue<Pair<Option<BatfishBgpRoute>, uint>> HijackRouteAndPrefix { get; } =
    new("hijackAndPrefix");

  private Hijack(Topology topology, string destination, string hijacker,
    Dictionary<string, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties)
    : base(topology, Transfer(topology, hijacker, HijackRouteAndPrefix.Value.Item2()),
      Merge(HijackRouteAndPrefix.Value.Item2()),
      topology.ForAllNodes(n => n == destination
        ? Pair.Create(Option.Create(BatfishBgpRouteExtensions.ToDestination(HijackRouteAndPrefix.Value.Item2())),
          Zen.False())
        : n == hijacker
          ? Pair.Create(HijackRouteAndPrefix.Value.Item1(), Zen.True())
          : Pair.Create<Option<BatfishBgpRoute>, bool>(Option.None<BatfishBgpRoute>(), Zen.False())
      ),
      annotations,
      stableProperties, safetyProperties, new BigInteger(4),
      new[] {HijackRouteAndPrefix})
  {
  }

  private static Func<Zen<TaggedRoute>, Zen<TaggedRoute>, Zen<TaggedRoute>> Merge(Zen<uint> destinationPrefix) =>
    Lang.MergeBy<TaggedRoute, Option<BatfishBgpRoute>>(
      Lang.Omap2<BatfishBgpRoute>((b1, b2) => b1.MinPrefix(b2, destinationPrefix)),
      t => t.Item1());

  /// <summary>
  /// Define the transfer function to filter all routes claiming to be from the
  /// destination prefix sent from the hijacker.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="hijacker"></param>
  /// <param name="destinationPrefix"></param>
  /// <returns></returns>
  private static Dictionary<(string, string), Func<Zen<TaggedRoute>, Zen<TaggedRoute>>> Transfer(Topology topology,
    string hijacker, Zen<uint> destinationPrefix) =>
    topology.ForAllEdges(e =>
      Lang.Product(
        Lang.Test(
          Lang.IfSome<BatfishBgpRoute>(b => Zen.And(b.GetDestination() == destinationPrefix, e.Item1 == hijacker)),
          Lang.Const(Option.None<BatfishBgpRoute>()),
          Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath)),
        Lang.Identity<bool>()));

  /// <summary>
  /// Add a hijacker node to the topology, connected to all of the core nodes.
  /// </summary>
  /// <param name="hijacker"></param>
  /// <param name="topology"></param>
  /// <returns></returns>
  private static Topology HijackTopology(string hijacker, Topology topology)
  {
    var withHijacker = topology.Neighbors;
    withHijacker[hijacker] = topology.Nodes.Where(n => n.IsCore()).ToList();
    foreach (var node in withHijacker[hijacker])
    {
      withHijacker[node].Add(hijacker);
    }

    return new Topology(withHijacker);
  }

  private static Func<Zen<TaggedRoute>, Zen<bool>> MapInternal(Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>> f) =>
    Lang.Both<Option<BatfishBgpRoute>, bool>(f, Zen.Not);

  private static Zen<bool> DestinationRouteIsInternal(Zen<TaggedRoute> r) =>
    Zen.Implies(HasDestinationRoute(r.Item1()), Zen.Not(r.Item2()));

  /// <summary>
  /// Return an annotation given a time step t.
  /// Until t, the route should not have a route to the destination.
  /// At and after t, the route should have a route to the destination and it should be internal.
  /// </summary>
  /// <param name="t">The time step at which a route to the destination is acquired.</param>
  /// <returns>An annotation for a tagged route.</returns>
  private static Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>> Annotate(BigInteger t)
  {
    return Lang.Until<TaggedRoute>(t,
      DestinationRouteIsInternal,
      // Lang.First<Option<BatfishBgpRoute>, bool>(o => o.Where(b => b.DestinationIs(DestinationPrefix)).IsNone()),
      p => Zen.And(DestinationRouteIsInternal(p), p.Item1().IsSome()));
    //MapInternal(HasDestinationRoute));
  }

  private static Zen<bool> HasDestinationRoute(Zen<Option<BatfishBgpRoute>> o) =>
    o.Where(b => b.DestinationIs(HijackRouteAndPrefix.Value.Item2())).IsSome();

  public static Hijack HijackFiltered(uint numPods, string destination)
  {
    const string hijackNode = "hijacker";
    var topology = HijackTopology(hijackNode, Topologies.FatTree(numPods));
    var distances = topology.BreadthFirstSearch(destination);
    Dictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations =
      distances.Select(p => (p.Key,
          // hijacker annotation is just true
          p.Key == hijackNode
            ? Lang.Globally(Lang.True<TaggedRoute>())
            : Lang.Intersect(
              Lang.Globally<TaggedRoute>(DestinationRouteIsInternal),
              //Zen.Implies(pair.Item1().IsSome(), MapInternal(HasDestinationRoute)(pair))),
              Lang.Finally(p.Value,
                MapInternal(
                  HasDestinationRoute))))) //Lang.Until(p.Value, DestinationRouteIsInternal, MapInternal(HasDestinationRoute))))
        .ToDictionary(p => p.Item1, p => p.Item2);
    IReadOnlyDictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<bool>>> stableProperties =
      topology.ForAllNodes(n => n == hijackNode ? Lang.True<TaggedRoute>() : MapInternal(HasDestinationRoute));
    IReadOnlyDictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<bool>>> safetyProperties =
      topology.ForAllNodes(_ => Lang.True<TaggedRoute>());
    return new Hijack(topology, destination, hijackNode, annotations, stableProperties, safetyProperties);
  }

  public static Hijack HijackFilteredOld(uint numPods, string destination)
  {
    const string hijackNode = "hijacker";
    var topology = HijackTopology(hijackNode, Topologies.FatTree(numPods));
    var distances = topology.BreadthFirstSearch(destination);
    Dictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations =
      distances.Select(p => (p.Key,
          // hijacker annotation is just true
          p.Key == hijackNode ? Lang.Globally(Lang.True<TaggedRoute>()) : Annotate(p.Value)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    IReadOnlyDictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<bool>>> stableProperties =
      topology.ForAllNodes(n =>
        n == hijackNode
          ? Lang.True<TaggedRoute>()
          : Lang.First<Option<BatfishBgpRoute>, bool>(Lang.IsSome<BatfishBgpRoute>()));
    IReadOnlyDictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<bool>>> safetyProperties =
      topology.ForAllNodes(n =>
        n == hijackNode
          ? Lang.True<TaggedRoute>()
          : p => Zen.Implies(HasDestinationRoute(p.Item1()), Zen.Not(p.Item2())));
    return new Hijack(topology, destination, hijackNode, annotations, stableProperties, safetyProperties);
  }
}

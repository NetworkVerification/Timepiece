using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

// a route which is tagged as internal (false) or external (true)
using TaggedRoute = Pair<Option<BatfishBgpRoute>, bool>;

public class Hijack : FatTree<TaggedRoute, Option<BatfishBgpRoute>>
{
  public SymbolicValue<Option<BatfishBgpRoute>> HijackRoute { get; } = new("hijack");
  public static Zen<uint> DestinationPrefix => Zen.Symbolic<uint>();

  protected Hijack(Topology topology, string destination, string hijacker,
    Dictionary<string, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties)
    : base(topology, destination, Transfer(topology, hijacker), Merge(DestinationPrefix),
      Pair.Create(Option.Create(BatfishBgpRouteExtensions.ToDestination(DestinationPrefix)),
        Zen.False()),
      Pair.Create<Option<BatfishBgpRoute>, bool>(Option.None<BatfishBgpRoute>(), Zen.False()), annotations,
      stableProperties, safetyProperties, Array.Empty<SymbolicValue<Option<BatfishBgpRoute>>>())
  {
    InitialValues[hijacker] = Pair.Create(HijackRoute.Value, Zen.True());
    Symbolics = new[] {HijackRoute};
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
  /// <returns></returns>
  private static Dictionary<(string, string), Func<Zen<TaggedRoute>, Zen<TaggedRoute>>> Transfer(Topology topology,
    string hijacker) =>
    topology.ForAllEdges(e =>
      Lang.Product(
        Lang.Test(
          Lang.IfSome<BatfishBgpRoute>(b => Zen.And(b.GetDestination() == DestinationPrefix, e.Item1 == hijacker)),
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

  private static Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>> Annotate(BigInteger dist)
  {
    return Lang.Until(dist,
      Lang.First<Option<BatfishBgpRoute>, bool>(o => o.Where(b => b.DestinationIs(DestinationPrefix)).IsNone()),
      MapInternal(o => o.Where(b => Zen.And(b.DestinationIs(DestinationPrefix), b.LpEquals(0), b.LengthAtMost(dist)))
        .IsSome()));
  }

  public static Hijack HijackFiltered(uint numPods, string destination)
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
          : p => Zen.Implies(p.Item1().Where(b => b.DestinationIs(DestinationPrefix)).IsSome(), Zen.Not(p.Item2())));
    return new Hijack(topology, destination, hijackNode, annotations, stableProperties, safetyProperties);
  }
}

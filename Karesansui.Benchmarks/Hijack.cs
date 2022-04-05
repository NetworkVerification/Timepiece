using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

// a route which is tagged as internal (false) or external (true)
using TaggedRoute = Pair<Option<BatfishBgpRoute>, bool>;

public class Hijack : FatTree<TaggedRoute, Option<BatfishBgpRoute>>
{
  public SymbolicValue<Option<BatfishBgpRoute>> HijackRoute { get; } = new("hijack");

  protected Hijack(Topology topology, string destination, string hijacker,
    Dictionary<string, Func<Zen<TaggedRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<TaggedRoute>, Zen<bool>>> safetyProperties)
    : base(topology, destination, Transfer(topology, hijacker), Merge,
      Pair.Create(Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()), Zen.False()),
      Pair.Create<Option<BatfishBgpRoute>, bool>(Option.None<BatfishBgpRoute>(), Zen.False()), annotations,
      stableProperties, safetyProperties, Array.Empty<SymbolicValue<Option<BatfishBgpRoute>>>())
  {
    InitialValues[hijacker] = Pair.Create(HijackRoute.Value, Zen.True());
    Symbolics = new[] {HijackRoute};
  }

  private static Zen<TaggedRoute> Merge(Zen<TaggedRoute> r1, Zen<TaggedRoute> r2) =>
    Zen.If(Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min)(r1.Item1(), r2.Item1()) == r1.Item1(), r1, r2);

  /// <summary>
  /// Define the transfer function to drop all routes originating from the hijacker.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="hijacker"></param>
  /// <returns></returns>
  private static Dictionary<(string, string), Func<Zen<TaggedRoute>, Zen<TaggedRoute>>> Transfer(Topology topology,
    string hijacker) =>
    topology.ForAllEdges(e =>
      Lang.Product(
        e.Item1 == hijacker
          ? Lang.Const(Option.None<BatfishBgpRoute>())
          : Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath),
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
    withHijacker[hijacker] = topology.Nodes.Where(n => n.StartsWith("core")).ToList();
    foreach (var node in withHijacker[hijacker])
    {
      withHijacker[node].Add(hijacker);
    }

    return new Topology(withHijacker);
  }

  public static Hijack HijackFiltered(uint numPods, string destination)
  {
    const string hijackNode = "hijacker";
    var topology = HijackTopology(hijackNode, Topologies.FatTree(numPods));
    var distances = topology.BreadthFirstSearch(destination);
    Dictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations =
      distances.Select(p => (p.Key,
          p.Key == hijackNode
            ? Lang.Globally(Lang.True<TaggedRoute>())
            : Lang.Until(p.Value, Lang.Both<Option<BatfishBgpRoute>, bool>(Option.IsNone, Zen.Not),
              Lang.Both<Option<BatfishBgpRoute>, bool>(BatfishBgpRouteExtensions.MaxLengthZeroLp(p.Value), Zen.Not))))
        .ToDictionary(p => p.Item1, p => p.Item2);
    IReadOnlyDictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<bool>>> stableProperties =
      topology.ForAllNodes(_ => Lang.First<Option<BatfishBgpRoute>, bool>(Lang.IsSome<BatfishBgpRoute>()));
    IReadOnlyDictionary<string, Func<Zen<Pair<Option<BatfishBgpRoute>, bool>>, Zen<bool>>> safetyProperties =
      topology.ForAllNodes(n =>
        n != hijackNode ? Lang.Second<Option<BatfishBgpRoute>, bool>(Zen.Not) : Lang.True<TaggedRoute>());
    return new Hijack(topology, destination, hijackNode, annotations, stableProperties, safetyProperties);
  }
}

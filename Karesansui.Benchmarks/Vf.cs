using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

public class Vf<TS> : FatTree<Option<BatfishBgpRoute>, TS>
{
  public Vf(LabelledTopology<int> topology, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    base(topology, destination, Transfer(topology),
      Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
      Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()),
      Option.None<BatfishBgpRoute>(),
      annotations, stableProperties, safetyProperties, symbolics)
  {
  }

  /// <summary>
  /// Export functionality.
  /// If tag1 is present, add tag2; otherwise, add tag1.
  /// </summary>
  /// <param name="r"></param>
  /// <param name="tag1"></param>
  /// <param name="tag2"></param>
  /// <returns></returns>
  private static Zen<Option<BatfishBgpRoute>> Export(Zen<BatfishBgpRoute> r, string tag1, string tag2)
  {
    return Option.Create(Zen.If(r.HasCommunity(tag1), r.AddCommunity(tag2), r.AddCommunity(tag1)));
  }

  /// <summary>
  /// Import functionality.
  /// If tag2 is present, drop the route.
  /// </summary>
  /// <param name="r">Some given route (possibly None).</param>
  /// <param name="tag2">The filtering tag.</param>
  /// <returns>The given route if it does not contain the tag, otherwise None.</returns>
  private static Zen<Option<BatfishBgpRoute>> Import(Zen<Option<BatfishBgpRoute>> r, string tag2)
  {
    return r.Where(b => Zen.Not(b.HasCommunity(tag2)));
  }

  /// <summary>
  /// Return the transfer function for the given topology.
  /// Composes the import and export functionality:
  /// we export the route, adding a tag from the source (and incrementing the path length),
  /// and then import the route, checking for the second tag of the sink.
  /// </summary>
  /// <param name="topology"></param>
  /// <returns></returns>
  private static Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>>
    Transfer(LabelledTopology<int> topology)
  {
    return topology.ForAllEdges(e =>
    {
      var (src, snk) = e;
      var srcTag1 = Vf.NodeTag1(topology, src);
      var srcTag2 = Vf.NodeTag2(topology, src);
      // on export, add tags and increment AS path
      var export = Lang.Bind<BatfishBgpRoute, BatfishBgpRoute>(b => Export(b.IncrementAsPath(), srcTag1, srcTag2));
      // on import, drop if tag2 is set
      var import =
        new Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>(r =>
          Import(r, Vf.NodeTag2(topology, snk)));
      // compose the export and import functions
      return Lang.Compose(export, import);
    });
  }

  private static Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>>
    Transfer2(Topology topology)
  {
    const string down = "down";
    const string up = "up";
    return topology.ForAllEdges(e =>
    {
      var increment = Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath);
      var (src, snk) = e;
      // use the down tag if the edge is pointing down, otherwise use the up tag
      var tag = ((src.StartsWith("aggregate") && snk.StartsWith("edge")) || src.StartsWith("core")) ? down : up;
      var addTag = Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(b =>
        b.WithCommunities(b.GetCommunities().Add(tag)));

      var import = Lang.Bind<BatfishBgpRoute, BatfishBgpRoute>(b =>
        Zen.If(Zen.And(b.HasCommunity(down), tag == up), Option.None<BatfishBgpRoute>(), Option.Create(b)));
      return Lang.Compose(increment, addTag, import);
    });
  }
}

public static class Vf
{
  /// <summary>
  /// Return a predicate for each node in the network, where its route is constrained
  /// to only contain the first tags of neighbors which are strictly closer to the destination than this node.
  /// </summary>
  /// <param name="topology">The topology containing the nodes.</param>
  /// <param name="distances">The distance from each node to the destination.</param>
  /// <returns>A dictionary from nodes to predicates over routes.</returns>
  private static Dictionary<string, Func<Zen<BatfishBgpRoute>, Zen<bool>>> ValidTags(LabelledTopology<int> topology,
    IReadOnlyDictionary<string, BigInteger> distances)
  {
    return topology.ForAllNodes(n =>
    {
      var dist = distances[n];
      var closerNodeTags = distances
        .Where(p => p.Value < dist)
        .Select(p => p.Key)
        .Aggregate(Set.Empty<string>(), (set, node) => set.Add(NodeTag1(topology, node)));
      return new Func<Zen<BatfishBgpRoute>, Zen<bool>>(b => b.GetCommunities().IsSubsetOf(closerNodeTags));
    });
  }

  public static Vf<Unit> ValleyFreeReachable(uint numPods, string destination)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var distances = topology.BreadthFirstSearch(destination);
    var hasValidTags = ValidTags(topology, distances);
    var annotations =
      new Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>>(distances.Select(p =>
        new KeyValuePair<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>>(p.Key,
          Lang.Until(p.Value, Lang.IsNone<BatfishBgpRoute>(),
            // require that the safety property holds at time t, and that the LP equals 0 and the path length equals t
            Lang.IfSome<BatfishBgpRoute>(b =>
              Zen.And(b.LpEquals(0U), b.GetAsPathLength() == p.Value, hasValidTags[p.Key](b)))
          ))));
    var safetyProperties =
      topology.ForAllNodes(n => Lang.Union(Lang.IfSome(hasValidTags[n]), Lang.IsNone<BatfishBgpRoute>()));
    var stableProperties = topology.ForAllNodes(_ => Lang.IsSome<BatfishBgpRoute>());
    return new Vf<Unit>(topology, destination, annotations, stableProperties, safetyProperties,
      Array.Empty<SymbolicValue<Unit>>());
  }

  internal static string NodeTag1(LabelledTopology<int> topology, string node)
  {
    if (node.StartsWith("core"))
    {
      return "3:0";
    }

    if (node.StartsWith("aggregate"))
    {
      return $"1:{topology.L(node)}";
    }

    if (node.StartsWith("edge"))
    {
      return $"2:{topology.L(node)}";
    }

    throw new ArgumentException($"Topology contains invalid node {node}");
  }

  internal static string NodeTag2(LabelledTopology<int> topology, string node)
  {
    if (node.StartsWith("core"))
    {
      return "6:0";
    }

    if (node.StartsWith("aggregate"))
    {
      return $"4:{topology.L(node)}";
    }

    if (node.StartsWith("edge"))
    {
      return $"5:{topology.L(node)}";
    }

    throw new ArgumentException($"Topology contains invalid node {node}");
  }
}

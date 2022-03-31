using System.Numerics;
using ZenLib;

namespace Karesansui.Benchmarks;

public class Vf<TS> : FatTree<TS>
{
  internal Vf(LabelledTopology<int> topology, string destination,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties, SymbolicValue<TS>[] symbolics) :
    base(topology, destination, Transfer(topology), annotations, safetyProperties, symbolics)
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
      return new Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>(r => import(export(r)));
    });
  }
}

public static class Vf
{
  /// <summary>
  /// Return a predicate for each node in the network, where its route is constrained
  /// to only contain the first tag of any of its neighbors.
  /// </summary>
  /// <param name="topology"></param>
  /// <returns></returns>
  private static Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> ValidTags(
    LabelledTopology<int> topology)
  {
    return topology.ForAllNodes(n =>
    {
      var neighborTags = topology[n].Select(nbr => NodeTag1(topology, nbr));
      var maximalSet = neighborTags.Aggregate(Set.Empty<string>(), (set, tag) => set.Add(tag));
      return Lang.IfSome<BatfishBgpRoute>(b =>
        // neighborTags.Aggregate(Zen.True(), (current, tag) => Zen.And(current, Zen.Not(b.HasCommunity(tag)))));
        b.GetCommunities().IsSubsetOf(maximalSet));
    });
  }

  public static Vf<Unit> ValleyFreeReachable(LabelledTopology<int> topology, string destination)
  {
    var distances = topology.BreadthFirstSearch(destination);
    var safetyProperties = ValidTags(topology);
    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value, Lang.IsNone<BatfishBgpRoute>(), safetyProperties[p.Key])))
        .ToDictionary(p => p.Item1, p => p.Item2);
    return new Vf<Unit>(topology, destination, annotations, topology.ForAllNodes(_ => Lang.IsSome<BatfishBgpRoute>()),
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

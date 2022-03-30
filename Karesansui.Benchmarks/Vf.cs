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

  private static Zen<Option<BatfishBgpRoute>> Export(Zen<BatfishBgpRoute> r, string tag1, string tag2)
  {
    return Option.Create(Zen.If(r.HasCommunity(tag1), r.AddCommunity(tag2), r.AddCommunity(tag1)));
  }

  private static Zen<Option<BatfishBgpRoute>> Import(Zen<BatfishBgpRoute> r, string tag2)
  {
    return Option.Create(r).Where(b => b.HasCommunity(tag2));
  }

  private static Dictionary<string, (string, string)> NodeTags(LabelledTopology<int> topology)
  {
    return topology.ForAllNodes(n =>
    {
      if (n.StartsWith("core"))
      {
        return ("3:0", "6:0");
      }

      if (n.StartsWith("aggregate"))
      {
        return ($"1:{topology.Labels[n]}", $"4:{topology.Labels[n]}");
      }

      if (n.StartsWith("edge"))
      {
        return ($"2:{topology.Labels[n]}", $"5:{topology.Labels[n]}");
      }

      throw new ArgumentException($"Topology contains invalid node {n}");
    });
  }

  private static Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>>
    Transfer(LabelledTopology<int> topology)
  {
    var tags = NodeTags(topology);
    return topology.ForAllEdges(e =>
    {
      var (srcTag1, srcTag2) = tags[e.Item1];
      var (_, snkTag2) = tags[e.Item2];
      // on export, add tags and increment AS path
      var export = Lang.Bind<BatfishBgpRoute, BatfishBgpRoute>(b => Export(b.IncrementAsPath(), srcTag1, srcTag2));
      // on import, drop if tag2 is set
      var import = Lang.Bind<BatfishBgpRoute, BatfishBgpRoute>(b => Import(b, snkTag2));
      // compose the export and import functions
      return new Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>(r => import(export(r)));
    });
  }
}

public static class Vf
{
  public static Vf<Unit> ValleyFreeReachable(LabelledTopology<int> topology, string destination)
  {
    var distances = topology.BreadthFirstSearch(destination);
    var reachable = Lang.IsSome<BatfishBgpRoute>();
    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value, Lang.IsNone<BatfishBgpRoute>(), reachable)))
        .ToDictionary(p => p.Item1, p => p.Item2);
    var safetyProperties = topology.ForAllNodes(_ => reachable);
    return new Vf<Unit>(topology, destination, annotations, safetyProperties, Array.Empty<SymbolicValue<Unit>>());
  }
}

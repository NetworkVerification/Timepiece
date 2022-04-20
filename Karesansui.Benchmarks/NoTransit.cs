using System.Numerics;
using Karesansui.Networks;
using ZenLib;

namespace Karesansui.Benchmarks;

public class NoTransit : Network<Option<BatfishBgpRoute>, bool>
{
  private static readonly SymbolicValue<bool>[] ExternalRelationships =
  {
    new("extSrc"),
    new("extDest")
  };

  public NoTransit(Topology topology, string externalSrc, string externalDest,
    Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>> transferFunction,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime) : base(topology, transferFunction,
    Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
    topology.ForAllNodes(n =>
      n == externalSrc ? Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()) : Option.None<BatfishBgpRoute>()),
    annotations, stableProperties, safetyProperties, convergeTime, ExternalRelationships)
  {
  }

  private static Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>> NoTransitProperty(
    IReadOnlyList<SymbolicValue<bool>> relationships)
  {
    return r => Zen.If(Zen.Or(relationships[0].Value, relationships[1].Value), r.IsSome(), r.IsNone());
  }

  public static NoTransit NoTransitSound(uint nodes)
  {
    var internalTopology = Topologies.Complete(nodes).Neighbors;
    const string externalSrc = "externalSrc";
    const string externalDest = "externalDest";
    // add an edge from the external source into the topology
    internalTopology["A"].Add(externalSrc);
    // add an edge to the external dest from the topology
    internalTopology.Add(externalDest, new List<string> {"B"});
    var topology = new Topology(internalTopology);
    var transfer = topology.ForAllEdges(e =>
      Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath));
    // TODO: need to check that the advertised route from externalSrc is destined to externalDest
    var stableProperties = topology.ForAllNodes(n =>
      n == externalDest ? NoTransitProperty(ExternalRelationships) : Lang.True<Option<BatfishBgpRoute>>());
    var safetyProperties = topology.ForAllNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var convergeTime = new BigInteger(4);
    var annotations = topology.ForAllNodes(n =>
    {
      if (n == externalSrc)
      {
        return Lang.Globally(Lang.True<Option<BatfishBgpRoute>>());
      }

      if (n == externalDest)
      {
        return Lang.Finally(convergeTime, NoTransitProperty(ExternalRelationships));
      }

      return Lang.Globally(Lang.True<Option<BatfishBgpRoute>>());
    });
    return new NoTransit(topology, externalSrc, externalDest, transfer, annotations, stableProperties, safetyProperties,
      convergeTime);
  }
}

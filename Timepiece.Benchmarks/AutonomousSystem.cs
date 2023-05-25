using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

public class AutonomousSystem<TS> : AnnotatedNetwork<Option<BgpRoute>, string, TS>
{
  public AutonomousSystem(Topology<string> topology, string externalSrc, string externalDest,
    Dictionary<(string, string), Func<Zen<Option<BgpRoute>>, Zen<Option<BgpRoute>>>> transferFunction,
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime, SymbolicValue<TS>[] symbolics) : base(topology, transferFunction,
    Lang.Omap2<BgpRoute>(BgpRouteExtensions.Min),
    topology.MapNodes(n =>
      n == externalSrc ? Option.Create<BgpRoute>(new BgpRoute()) : Option.None<BgpRoute>()),
    annotations, stableProperties, safetyProperties, convergeTime, symbolics)
  {
  }

  private static Func<Zen<Option<BgpRoute>>, Zen<bool>> NoTransitProperty(
    IReadOnlyList<SymbolicValue<bool>> relationships)
  {
    return r => Zen.Implies(Zen.And(Zen.Not(relationships[0].Value), Zen.Not(relationships[1].Value)), r.IsNone());
  }

  private static Topology<string> AsWithExternalPeers(uint nodes, int peers)
  {
    var internalTopology = Topologies.Complete(nodes);
    var newNeighbors = internalTopology.Neighbors;
    for (var i = 0; i < peers; i++)
    {
      var externalNode = $"external-{i}";
      var currentNode = internalTopology.Nodes[i % internalTopology.Nodes.Length];
      newNeighbors[currentNode].Add(externalNode);
      newNeighbors.Add(externalNode, new List<string> {currentNode});
    }

    return new Topology<string>(newNeighbors);
  }

  // public static AutonomousSystem<Option<BgpRoute>> BlockToExternal(uint nodes)
  // {
  // const string BTE = "BTE";
  // var topology = AsWithExternalPeers(nodes, 2);
  // var sent = new SymbolicValue<Option<BgpRoute>>("sent");
  // var transfer = topology.MapEdges(e =>
  // Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath));
  // var initialValues = topology.MapNodes(n => n == "external-0" ? sent.Value : Option.None<BgpRoute>());
  // var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
  // var stableProperties = topology.MapNodes(n =>
  // Lang.IfSome(Zen.Implies(n == "external-1", sent.Value.Where(b => Zen.Not(b.HasCommunity(BTE))).IsNone())));
  // }

  public static AutonomousSystem<bool> NoTransitSound(uint nodes)
  {
    var internalTopology = Topologies.Complete(nodes).Neighbors;
    const string externalSrc = "externalSrc";
    const string externalDest = "externalDest";
    // add an edge from the external source into the topology
    internalTopology["A"].Add(externalSrc);
    // add an edge to the external dest from the topology
    internalTopology.Add(externalDest, new List<string> {"B"});
    var topology = new Topology<string>(internalTopology);
    var transfer = topology.MapEdges(e =>
      e switch
      {
        ("externalSrc", "A") => Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath),
        ("B", "externalDest") => Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath),
        _ => Lang.Identity<Option<BgpRoute>>()
      });
    // TODO: need to check that the advertised route from externalSrc is destined to externalDest
    var externalRelationships = new SymbolicValue<bool>[]
    {
      new("extSrc"),
      new("extDest")
    };
    var stableProperties = topology.MapNodes(n =>
      n == externalDest ? NoTransitProperty(externalRelationships) : Lang.True<Option<BgpRoute>>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var convergeTime = new BigInteger(4);
    var annotations = topology.MapNodes(n =>
    {
      if (n == externalSrc) return Lang.Globally(Lang.True<Option<BgpRoute>>());

      if (n == externalDest) return Lang.Globally(NoTransitProperty(externalRelationships));

      return Lang.Globally(Lang.True<Option<BgpRoute>>());
    });
    return new AutonomousSystem<bool>(topology, externalSrc, externalDest, transfer, annotations, stableProperties,
      safetyProperties,
      convergeTime, externalRelationships);
  }
}

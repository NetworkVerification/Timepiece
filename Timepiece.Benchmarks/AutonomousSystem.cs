using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Benchmarks;

public class AutonomousSystem<TS> : Network<Option<BatfishBgpRoute>, TS>
{
  public AutonomousSystem(Topology topology, string externalSrc, string externalDest,
    Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>> transferFunction,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime, SymbolicValue<TS>[] symbolics) : base(topology, transferFunction,
    Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
    topology.MapNodes(n =>
      n == externalSrc ? Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()) : Option.None<BatfishBgpRoute>()),
    annotations, stableProperties, safetyProperties, convergeTime, symbolics)
  {
  }

  private static Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>> NoTransitProperty(
    IReadOnlyList<SymbolicValue<bool>> relationships)
  {
    return r => Zen.Implies(Zen.And(Zen.Not(relationships[0].Value), Zen.Not(relationships[1].Value)), r.IsNone());
  }

  private static Topology AsWithExternalPeers(uint nodes, int peers)
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

    return new Topology(newNeighbors);
  }

  // public static AutonomousSystem<Option<BatfishBgpRoute>> BlockToExternal(uint nodes)
  // {
  // const string BTE = "BTE";
  // var topology = AsWithExternalPeers(nodes, 2);
  // var sent = new SymbolicValue<Option<BatfishBgpRoute>>("sent");
  // var transfer = topology.MapEdges(e =>
  // Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath));
  // var initialValues = topology.MapNodes(n => n == "external-0" ? sent.Value : Option.None<BatfishBgpRoute>());
  // var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
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
    var topology = new Topology(internalTopology);
    var transfer = topology.MapEdges(e =>
      e switch
      {
        ("externalSrc", "A") => Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath),
        ("B", "externalDest") => Lang.Omap<BatfishBgpRoute, BatfishBgpRoute>(BatfishBgpRouteExtensions.IncrementAsPath),
        _ => Lang.Identity<Option<BatfishBgpRoute>>()
      });
    // TODO: need to check that the advertised route from externalSrc is destined to externalDest
    var externalRelationships = new SymbolicValue<bool>[]
    {
      new("extSrc"),
      new("extDest")
    };
    var stableProperties = topology.MapNodes(n =>
      n == externalDest ? NoTransitProperty(externalRelationships) : Lang.True<Option<BatfishBgpRoute>>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BatfishBgpRoute>>());
    var convergeTime = new BigInteger(4);
    var annotations = topology.MapNodes(n =>
    {
      if (n == externalSrc)
      {
        return Lang.Globally(Lang.True<Option<BatfishBgpRoute>>());
      }

      if (n == externalDest)
      {
        return Lang.Globally(NoTransitProperty(externalRelationships));
      }

      return Lang.Globally(Lang.True<Option<BatfishBgpRoute>>());
    });
    return new AutonomousSystem<bool>(topology, externalSrc, externalDest, transfer, annotations, stableProperties,
      safetyProperties,
      convergeTime, externalRelationships);
  }
}

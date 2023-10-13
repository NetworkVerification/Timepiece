using System.Collections.Immutable;
using Timepiece.Angler.Ast;

namespace Timepiece.Angler.Tests;

public static class AnglerNetworkTests
{
  private static readonly Dictionary<string, NodeProperties> SmallMesh = new()
  {
    {"A", new NodeProperties(new[] {"B", "C", "123.123.45.45"})},
    {"B", new NodeProperties(new[] {"A", "C"})},
    {"C", new NodeProperties(new[] {"A", "B"})},
  };

  private static readonly List<ExternalPeer> ExternalPeers = new()
  {
    new ExternalPeer("123.123.45.45", new[] {"A"}),
  };

  private static readonly AnglerNetwork SmallMeshNetwork = new(SmallMesh, ExternalPeers);

  [Fact]
  public static void SmallMeshTopologyContainsAllNodes()
  {
    var externalNodes = ExternalPeers.Select(p => p.Name);
    var nodes = externalNodes.Concat(SmallMesh.Keys);
    var (topology, _) = SmallMeshNetwork.TopologyAndTransfer();
    Assert.Equivalent(nodes, topology.Nodes, strict: true);
  }

  [Fact]
  public static void SmallMeshTopologyContainsAllEdges()
  {
    var edges = SmallMesh
      .SelectMany(p => p.Value.Policies.Keys.Select(m => (src: m, snk: p.Key)));
    // the AnglerNetwork will automatically add any additional external back-edges
    var externalEdges = ExternalPeers
      .SelectMany(p => edges.Where(e => e.src.Equals(p.Name)).Select(e => (e.snk, e.src)));
    var allExpectedEdges = edges.Concat(externalEdges);
    var (topology, _) = SmallMeshNetwork.TopologyAndTransfer();
    var allActualEdges = topology.FoldEdges(ImmutableList<(string Key, string m)>.Empty, (l, e) => l.Add(e));
    Assert.Equivalent(allExpectedEdges, allActualEdges, strict: true);
  }

  [Fact]
  public static void SmallMeshTransferContainsAllEdges()
  {
    var edges = SmallMesh
      .SelectMany(p => p.Value.Policies.Keys.Select(m => (src: m, snk: p.Key)));
    // the AnglerNetwork will automatically add any additional external back-edges
    var externalEdges = ExternalPeers
      .SelectMany(p => edges.Where(e => e.src.Equals(p.Name)).Select(e => (e.snk, e.src)));
    var allExpectedEdges = edges.Concat(externalEdges);
    var (_, transfer) = SmallMeshNetwork.TopologyAndTransfer();
    var allActualEdges = transfer.Keys;
    Assert.Equivalent(allExpectedEdges, allActualEdges, strict: true);
  }
}

using System.Linq;
using Xunit;

namespace Timepiece.Tests;

public class DigraphTests
{
  public static TheoryData<Digraph<string>, string[]> InducedSubgraphs => new()
  {
    {Topologies.Complete(5), System.Array.Empty<string>()},
    {Topologies.Complete(5), new[] {"A"}},
    {Topologies.Complete(5), new[] {"A", "B", "C"}},
  };

  public static TheoryData<Digraph<string>, string[], (string, string)[]> InducedSubgraphsEdges => new()
  {
    {Topologies.Complete(5), System.Array.Empty<string>(), System.Array.Empty<(string, string)>()},
    {Topologies.Complete(5), new[] {"A"}, System.Array.Empty<(string, string)>()},
    {Topologies.Complete(5), new[] {"A", "B", "C"}, new[] {("A", "B")}},
  };

  [Theory]
  [InlineData(2)]
  [InlineData(4)]
  [InlineData(5)]
  public void CompleteGraphInducedSubgraphIsCompleteGraph(uint nodes)
  {
    var g1 = Topologies.Complete(nodes);
    var g2 = Topologies.Complete(nodes - 1);
    Assert.Equal(g2.Neighbors, g1.InducedSubgraph(g1.Nodes.Take((int) nodes - 1)).Neighbors);
  }

  [Theory]
  [MemberData(nameof(InducedSubgraphs))]
  public void InducedSubgraphHasCorrectNumberOfNodes(Digraph<string> g, string[] subset)
  {
    var sub = g.InducedSubgraph(subset);
    Assert.Equal(subset.Length, sub.NNodes);
  }

  [Theory]
  [MemberData(nameof(InducedSubgraphsEdges))]
  public void InducedSubgraphHasCorrectEdges(Digraph<string> g, string[] subset, (string, string)[] edges)
  {
    var sub = g.InducedSubgraph(subset);
    Assert.Equivalent(edges, sub.Edges().ToArray());
  }
}

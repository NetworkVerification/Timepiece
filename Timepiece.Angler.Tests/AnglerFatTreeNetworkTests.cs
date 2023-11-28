using Timepiece.Angler.Networks;

namespace Timepiece.Angler.Tests;

public class AnglerFatTreeNetworkTests
{
  public static TheoryData<Digraph<string>, string> LastEdgeNodes => new()
  {
    {Topologies.FatTree(4), "edge-19"},
    {Topologies.FatTree(8), "edge-79"},
    {Topologies.FatTree(12), "edge-179"},
    {Topologies.FatTree(16), "edge-319"},
    {Topologies.FatTree(20), "edge-499"},
  };

  [Theory]
  [MemberData(nameof(LastEdgeNodes))]
  public void LastEdgeNodeCorrect(Digraph<string> digraph, string expected)
  {
    Assert.Equal(expected, AnglerFatTreeNetwork.LastEdgeNode(digraph));
  }
}

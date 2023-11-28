using System.Linq;
using Xunit;

namespace Timepiece.Tests;

public class FatTreeTests
{
  public static TheoryData<Digraph<string>, int> FatTreeDigraphs => new()
  {
    {Topologies.FatTree(4), 4}
  };

  [Theory]
  [InlineData(4)]
  [InlineData(8)]
  [InlineData(12)]
  public void InfersCorrectNumberOfPodsRegularFatTree(uint numPods)
  {
    var fatTree = Topologies.FatTree(numPods);
    var labelled = FatTree.LabelFatTree(fatTree);
    Assert.Equal(numPods - 1, (uint) labelled.Labels.Where(p => p.Key.IsAggregation()).Max(p => p.Value));
  }
}

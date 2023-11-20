using Timepiece.Angler.Networks;

namespace Timepiece.Angler.Tests;

public class QueryTypeTests
{
  [Fact]
  public void AllQueryTypesHaveUniqueShorthands()
  {
    Assert.Distinct(Enum.GetValues<QueryType>().Select(qt => qt.ShortHand()));
  }
}

using Timepiece.Angler.Networks;

namespace Timepiece.Angler.Tests;

public class QueryTypeTests
{
  private static readonly QueryType[] Values = Enum.GetValues<QueryType>();

  [Fact]
  public void AllQueryTypesHaveUniqueShorthands()
  {
    Assert.Distinct(Values.Select(qt => qt.ShortHand()));
  }

  [Fact]
  public void AllQueryValuesAreAcceptableNames()
  {
    Assert.All(Values, qt => Assert.Equal(qt, $"{qt}".ToQueryType()));
  }
}

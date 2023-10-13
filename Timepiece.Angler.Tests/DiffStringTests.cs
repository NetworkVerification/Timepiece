namespace Timepiece.Angler.Tests;

public static class DiffStringTests
{
  [Theory]
  [InlineData(false, false, false, false, "RouteResult()")]
  [InlineData(true, false, false, false, "RouteResult(Exit=True)")]
  [InlineData(true, false, true, false, "RouteResult(Exit=True,Returned=True)")]
  [InlineData(false, false, true, true, "RouteResult(Returned=True,Value=True)")]
  public static void RouteResultDiffStrings(bool exit, bool fallthrough, bool returned, bool value, string expected)
  {
    Assert.Equal(expected, new RouteResult(exit, fallthrough, returned, value).DiffString());
  }

  [Theory]
  [InlineData(RouteEnvironment.DefaultLp, "RouteEnvironment()")]
  [InlineData(200, "RouteEnvironment(Lp=200)")]
  [InlineData(0, "RouteEnvironment(Lp=0)")]
  public static void RouteEnvironmentDiffStringsVaryLp(uint lp, string expected)
  {
    Assert.Equal(expected, new RouteEnvironment {Lp = lp}.DiffString());
  }
}

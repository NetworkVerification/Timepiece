using ZenLib;

namespace Timepiece.Angler.Tests;

public static class RouteEnvironmentTests
{
  public static TheoryData<string, RouteEnvironment> RouteEnvironmentStrings => new()
  {
    {
      "RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/32, Weight=32768, Lp=100, AsSet={}, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})",
      new RouteEnvironment()
    },
    {
      "RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/32, Weight=32768, Lp=100, AsSet={A, B}, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})",
      new RouteEnvironment { AsSet = new CSet<string>("A", "B")}
    }
  };

  [Theory]
  [MemberData(nameof(RouteEnvironmentStrings))]
  public static void TestRouteEnvironmentToString(string expected, RouteEnvironment env)
  {
    Assert.Equal(expected, env.ToString());
  }
}

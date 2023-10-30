using Timepiece.Angler.DataTypes;
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
      new RouteEnvironment {AsSet = new CSet<string>("A", "B")}
    }
  };

  [Theory]
  [MemberData(nameof(RouteEnvironmentStrings))]
  public static void TestRouteEnvironmentToString(string expected, RouteEnvironment env)
  {
    Assert.Equal(expected, env.ToString());
  }

  /// <summary>
  /// Theory data asking if, for all RouteEnvironments, is the given community tag in the environment?
  /// </summary>
  public static TheoryData<Zen<RouteEnvironment>, string, bool> CommunityInEnvironment => new()
  {
    {Zen.Symbolic<RouteEnvironment>(), "A", false},
    {Zen.Constant(new RouteEnvironment()), "A", false},
    {Zen.Constant(new RouteEnvironment {Communities = new CSet<string>("B")}), "A", false},
    {Zen.Constant(new RouteEnvironment {Communities = new CSet<string>("A")}), "A", true},
    {Zen.Constant(new RouteEnvironment {Communities = new CSet<string>("B", "A")}), "A", true},
  };

  [Theory]
  [MemberData(nameof(CommunityInEnvironment))]
  public static void TestRouteEnvironmentContainsCommunity(Zen<RouteEnvironment> env, string community,
    bool expectedValid)
  {
    var b = Zen.Not(env.GetCommunities().Contains(community)).Solve();
    var solution = b.IsSatisfiable() ? b.Get(env) : null;
    if (expectedValid)
    {
      Assert.Null(solution);
    }
    else
    {
      Assert.NotNull(solution);
    }
  }
}

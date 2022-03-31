using Xunit;
using ZenLib;

namespace Karesansui.Benchmarks;

public static class BatfishBgpRouteTests
{
  [Fact]
  public static void MinPicksLargerLp()
  {
    var r1 = Zen.Symbolic<BatfishBgpRoute>();
    var r2 = Zen.Symbolic<BatfishBgpRoute>();
    var test = Zen.Implies(r1.GetLp() > r2.GetLp(), r1.Min(r2) == r1);
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPicksShorterPathLength()
  {
    var r1 = Zen.Symbolic<BatfishBgpRoute>();
    var r2 = Zen.Symbolic<BatfishBgpRoute>();
    var test = Zen.Implies(Zen.And(r1.GetLp() == r2.GetLp(), r1.GetAsPathLength() < r2.GetAsPathLength()),
      r1.Min(r2) == r1);
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPicksBetterOrigin()
  {
    var r1 = Zen.Symbolic<BatfishBgpRoute>();
    var r2 = Zen.Symbolic<BatfishBgpRoute>();
    var test = Zen.Implies(
      Zen.And(r1.GetLp() == r2.GetLp(), r1.GetAsPathLength() == r2.GetAsPathLength(),
        r1.GetOriginType() > r2.GetOriginType()),
      r1.Min(r2) == r1);
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPicksLowerMed()
  {
    var r1 = Zen.Symbolic<BatfishBgpRoute>();
    var r2 = Zen.Symbolic<BatfishBgpRoute>();
    var test = Zen.Implies(
      Zen.And(r1.GetLp() == r2.GetLp(), r1.GetAsPathLength() == r2.GetAsPathLength(),
        r1.GetOriginType() == r2.GetOriginType(), r1.GetMed() < r2.GetMed()),
      r1.Min(r2) == r1);
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }
}

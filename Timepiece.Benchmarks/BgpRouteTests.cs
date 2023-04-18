using Xunit;
using ZenLib;

namespace Timepiece.Benchmarks;

public static class BgpRouteTests
{
  [Fact]
  public static void MinPicksLargerLp()
  {
    var r1 = Zen.Symbolic<BgpRoute>();
    var r2 = Zen.Symbolic<BgpRoute>();

    Zen<bool> TestLp(Zen<BgpRoute> b1, Zen<BgpRoute> b2)
    {
      return Zen.Implies(
        b1.GetLp() > b2.GetLp(), b1.Min(b2) == b1);
    }

    var test = Zen.And(TestLp(r1, r2), TestLp(r2, r1));
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPicksShorterPathLength()
  {
    var r1 = Zen.Symbolic<BgpRoute>();
    var r2 = Zen.Symbolic<BgpRoute>();

    Zen<bool> TestPathLength(Zen<BgpRoute> b1, Zen<BgpRoute> b2)
    {
      return Zen.Implies(
        Zen.And(b1.GetLp() == b2.GetLp(), b1.GetAsPathLength() < b2.GetAsPathLength()), b1.Min(b2) == b1);
    }

    var test = Zen.And(TestPathLength(r1, r2), TestPathLength(r2, r1));
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPicksBetterOrigin()
  {
    var r1 = Zen.Symbolic<BgpRoute>();
    var r2 = Zen.Symbolic<BgpRoute>();

    Zen<bool> TestOrigin(Zen<BgpRoute> b1, Zen<BgpRoute> b2)
    {
      return Zen.Implies(
        Zen.And(b1.GetLp() == b2.GetLp(), b1.GetAsPathLength() == b2.GetAsPathLength(),
          b1.GetOriginType() > b2.GetOriginType()),
        b1.Min(b2) == b1);
    }

    var test = Zen.And(TestOrigin(r1, r2), TestOrigin(r2, r1));
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPicksLowerMed()
  {
    var r1 = Zen.Symbolic<BgpRoute>();
    var r2 = Zen.Symbolic<BgpRoute>();

    Zen<bool> TestMed(Zen<BgpRoute> b1, Zen<BgpRoute> b2)
    {
      return Zen.Implies(
        Zen.And(b1.GetLp() == b2.GetLp(), b1.GetAsPathLength() == b2.GetAsPathLength(),
          b1.GetOriginType() == b2.GetOriginType(), b1.GetMed() < b2.GetMed()),
        b1.Min(b2) == b1);
    }

    var test = Zen.And(TestMed(r1, r2), TestMed(r2, r1));
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPrefixEqualDestinations()
  {
    var d1 = Zen.Symbolic<uint>();
    var d2 = Zen.Symbolic<uint>();
    var r1 = BgpRouteExtensions.ToDestination(d1).WithLp(200);
    var r2 = BgpRouteExtensions.ToDestination(d2);

    var test = Zen.Implies(d1 == d2, Zen.And(r2.MinPrefix(r1, d1) == r1, r1.MinPrefix(r2, d1) == r1));
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }

  [Fact]
  public static void MinPrefixDifferentDestinations()
  {
    var d1 = Zen.Symbolic<uint>();
    var d2 = Zen.Symbolic<uint>();
    var r1 = BgpRouteExtensions.ToDestination(d1).WithLp(200);
    var r2 = BgpRouteExtensions.ToDestination(d2);

    var test = Zen.Implies(d1 != d2, Zen.And(r2.MinPrefix(r1, d1) == r1, r1.MinPrefix(r2, d1) == r1));
    Assert.False(Zen.Not(test).Solve().IsSatisfiable());
  }
}

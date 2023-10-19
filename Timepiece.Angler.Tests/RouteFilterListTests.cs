using Timepiece.Angler.Ast;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Tests;

public static class RouteFilterListTests
{
  public static TheoryData<RouteFilterLine[], Zen<Ipv4Prefix>> PermitsTheoryData => new()
  {
    {
      new[]
      {
        // matches everything
        new RouteFilterLine(true, new Ipv4Wildcard("0.0.0.0", "255.255.255.255"), new UInt<_6>(0), new UInt<_6>(32))
      },
      // any prefix
      Zen.Symbolic<Ipv4Prefix>()
    },
    {
      new[]
      {
        // matches only 192.168.0.0
        new RouteFilterLine(true, new Ipv4Wildcard("192.168.0.0", "0.0.0.0"), new UInt<_6>(32), new UInt<_6>(32))
      },
      // the prefix 192.168.0.0/32
      Zen.Constant(new Ipv4Prefix("192.168.0.0"))
    },
    {
      new[]
      {
        // denies 192.168.0.0, permits 192.168.0.1
        new RouteFilterLine(false, new Ipv4Wildcard("192.168.0.0", "0.0.0.0"), new UInt<_6>(32), new UInt<_6>(32)),
        new RouteFilterLine(true, new Ipv4Wildcard("192.168.0.1", "0.0.0.0"), new UInt<_6>(32), new UInt<_6>(32))
      },
      // the prefix 192.168.0.1/32
      Zen.Constant(new Ipv4Prefix("192.168.0.1"))
    }
  };

  [Fact]
  public static void TestEmptyListDenies()
  {
    var list = new RouteFilterList();
    var p = Zen.Symbolic<Ipv4Prefix>();
    Assert.False(list.Permits(p).Solve().IsSatisfiable());
  }

  [Theory]
  [MemberData(nameof(PermitsTheoryData))]
  public static void TestListPermits(RouteFilterLine[] lines, Zen<Ipv4Prefix> prefix)
  {
    var list = new RouteFilterList(lines);
    // check that it is *not* possible for Permits to ever return false, i.e. it always returns true
    // note that we have to constrain the prefix length to be at most 32
    Assert.False(Zen.And(prefix.GetPrefixLength() <= Zen.Constant(new UInt<_6>(32)),
      Zen.Not(list.Permits(prefix))).Solve().IsSatisfiable());
  }
}

using Newtonsoft.Json;
using Timepiece.Angler.Ast;
using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Tests;

public static class RouteFilterListTests
{
  public static TheoryData<RouteFilterLine[], Zen<Ipv4Prefix>, bool> MatchesTheoryData => new()
  {
    {
      new[]
      {
        // matches everything
        new RouteFilterLine(true, new Ipv4Wildcard("0.0.0.0", "255.255.255.255"), new UInt<_6>(0), new UInt<_6>(32))
      },
      // any prefix
      Zen.Symbolic<Ipv4Prefix>(),
      true
    },
    {
      new[]
      {
        // matches only 192.168.0.0
        new RouteFilterLine(true, new Ipv4Wildcard("192.168.0.0", "0.0.0.0"), new UInt<_6>(32), new UInt<_6>(32))
      },
      // the prefix 192.168.0.0/32
      Zen.Constant(new Ipv4Prefix("192.168.0.0")),
      true
    },
    {
      new[]
      {
        // denies 192.168.0.0, permits 192.168.0.1
        new RouteFilterLine(false, new Ipv4Wildcard("192.168.0.0", "0.0.0.0"), new UInt<_6>(32), new UInt<_6>(32)),
        new RouteFilterLine(true, new Ipv4Wildcard("192.168.0.1", "0.0.0.0"), new UInt<_6>(32), new UInt<_6>(32))
      },
      // the prefix 192.168.0.1/32
      Zen.Constant(new Ipv4Prefix("192.168.0.1")),
      true
    },
    {
      new[]
      {
        new RouteFilterLine(false, new Ipv4Wildcard("70.0.0.0", "0.255.255.255"), new UInt<_6>(8), new UInt<_6>(32)),
        new RouteFilterLine(false, new Ipv4Wildcard("10.0.0.0", "0.255.255.255"), new UInt<_6>(8), new UInt<_6>(32)),
        new RouteFilterLine(true, new Ipv4Wildcard("0.0.0.0", "255.255.255.255"), new UInt<_6>(0), new UInt<_6>(32))
      },
      Zen.Constant(new Ipv4Prefix("70.0.7.0/24")),
      false
    }
  };

  private const string MartiansRouteFilterList = """
                                                 {
                                                                                 "$type": "RouteFilterList",
                                                                                 "Lines": [
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 0,
                                                                                     "MinLength": 0,
                                                                                     "Wildcard": {
                                                                                       "Begin": "0.0.0.0",
                                                                                       "HostMask": "255.255.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 8,
                                                                                     "Wildcard": {
                                                                                       "Begin": "10.0.0.0",
                                                                                       "HostMask": "0.255.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 8,
                                                                                     "Wildcard": {
                                                                                       "Begin": "127.0.0.0",
                                                                                       "HostMask": "0.255.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 16,
                                                                                     "Wildcard": {
                                                                                       "Begin": "169.254.0.0",
                                                                                       "HostMask": "0.0.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 12,
                                                                                     "Wildcard": {
                                                                                       "Begin": "172.16.0.0",
                                                                                       "HostMask": "0.15.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 24,
                                                                                     "Wildcard": {
                                                                                       "Begin": "192.0.2.0",
                                                                                       "HostMask": "0.0.0.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 32,
                                                                                     "Wildcard": {
                                                                                       "Begin": "192.88.99.1",
                                                                                       "HostMask": "0.0.0.0"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 16,
                                                                                     "Wildcard": {
                                                                                       "Begin": "192.168.0.0",
                                                                                       "HostMask": "0.0.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 15,
                                                                                     "Wildcard": {
                                                                                       "Begin": "198.18.0.0",
                                                                                       "HostMask": "0.1.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 4,
                                                                                     "Wildcard": {
                                                                                       "Begin": "224.0.0.0",
                                                                                       "HostMask": "15.255.255.255"
                                                                                     }
                                                                                   },
                                                                                   {
                                                                                     "Action": true,
                                                                                     "MaxLength": 32,
                                                                                     "MinLength": 4,
                                                                                     "Wildcard": {
                                                                                       "Begin": "240.0.0.0",
                                                                                       "HostMask": "15.255.255.255"
                                                                                     }
                                                                                   }
                                                                                 ]
                                                                               }
                                                 """;

  [Fact]
  public static void TestEmptyListDenies()
  {
    var list = new RouteFilterList();
    var p = Zen.Symbolic<Ipv4Prefix>();
    Assert.False(list.Matches(p).Solve().IsSatisfiable());
  }

  [Theory]
  [MemberData(nameof(MatchesTheoryData))]
  public static void TestListMatches(RouteFilterLine[] lines, Zen<Ipv4Prefix> prefix, bool expected)
  {
    var list = new RouteFilterList(lines);
    // check that it is *not* possible for Matches to ever contradict the expected result
    // note that we have to constrain the prefix length to be at most 32
    var assumption = prefix.IsValidPrefixLength();
    Assert.False(expected
      ? Zen.And(assumption, Zen.Not(list.Matches(prefix))).Solve().IsSatisfiable()
      : Zen.And(assumption, list.Matches(prefix)).Solve().IsSatisfiable());
  }

  [Fact]
  public static void TestNonMartians()
  {
    var list = AstSerializationBinder.JsonSerializer()
      .Deserialize<RouteFilterList>(new JsonTextReader(new StringReader(MartiansRouteFilterList)));
    var nonMartianPrefix = Zen.Constant(new Ipv4Prefix("35.0.0.0", "35.255.255.255"));
    // check that the prefix matches -- we expect it not to
    var query = list.Matches(nonMartianPrefix).Solve();
    Assert.Null(query.IsSatisfiable() ? query.Get(nonMartianPrefix) : null);
  }
}

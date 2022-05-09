using NetTools;
using Timepiece.Datatypes;
using Xunit;

namespace Timepiece.Tests;

public static class Ipv4PrefixTests
{
  [Theory]
  [InlineData("127.0.0.1/32")]
  [InlineData("127.0.0.0/24")]
  public static void Ipv4PrefixToStringInvertible(string s)
  {
    var p = new Ipv4Prefix(s);
    Assert.Equal(s, p.ToString());
  }

  [Theory]
  [InlineData("127.0.0.0/24", "127.0.0.1/32")]
  [InlineData("70.0.19.0/24", "70.0.19.1")]
  public static void IpAddressRangeContainsIpv4Prefix(string range, string prefix)
  {
    var r = IPAddressRange.Parse(range);
    var p = new Ipv4Prefix(prefix);
    Assert.True(r.Contains(p));
  }
}

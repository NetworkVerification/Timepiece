using System.Net;
using NetTools;
using Timepiece.DataTypes;
using Xunit;

namespace Timepiece.Tests;

public class Ipv4PrefixTests
{
  [Theory]
  [InlineData("0.0.0.0", 0)]
  [InlineData("0.0.0.1", 1)]
  [InlineData("0.0.0.2", 2)]
  [InlineData("1.2.3.4", 16909060)]
  [InlineData("127.0.0.1", 2130706433)]
  public void IpAddressToUnsignedInt(string address, uint expected)
  {
    var actual = IPAddress.Parse(address).ToUnsignedInt();
    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData("127.0.0.1/32")]
  [InlineData("127.0.0.0/24")]
  [InlineData("8.28.178.0/23")]
  [InlineData("0.0.0.0/0")]
  public void Ipv4PrefixToStringInvertible(string s)
  {
    var p = new Ipv4Prefix(s);
    Assert.Equal(s, p.ToString());
  }

  [Theory]
  [InlineData("127.0.0.1/32")]
  [InlineData("127.0.0.0/24")]
  [InlineData("8.28.178.0/23")]
  [InlineData("0.0.0.0/0", Skip = "weirdly too slow?")]
  public void RoundTripAddressRangeIpv4Prefix(string s)
  {
    var r = IPAddressRange.Parse(s);
    Assert.Equal(r, new Ipv4Prefix(r).AsAddressRange());
  }

  [Theory]
  [InlineData("127.0.0.0/24", "127.0.0.1/32")]
  [InlineData("70.0.19.0/24", "70.0.19.1")]
  [InlineData("70.0.19.0/24", "70.0.19.0")]
  public void IpAddressRangeContainsIpv4Prefix(string range, string prefix)
  {
    var r = IPAddressRange.Parse(range);
    var p = new Ipv4Prefix(prefix);
    Assert.True(r.Contains(p));
  }

  [Theory]
  [InlineData("0.0.0.0/0")]
  [InlineData("1.2.3.4/32")]
  [InlineData("127.0.0.0/24")]
  [InlineData("70.0.19.0/24")]
  [InlineData("70.0.19.0/25")]
  [InlineData("70.0.19.0/26")]
  [InlineData("70.0.19.0/27")]
  [InlineData("70.0.19.1/32")]
  [InlineData("255.255.255.255/32")]
  public void RoundTripViaWildcard(string address)
  {
    var prefix = new Ipv4Prefix(address);
    Assert.Equal(prefix, prefix.ToWildcard().ToPrefix());
  }
}

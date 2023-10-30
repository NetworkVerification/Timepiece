using System.Net;
using NetTools;
using Timepiece.DataTypes;
using Xunit;

namespace Timepiece.Tests;

public static class Ipv4PrefixTests
{
  [Theory]
  [InlineData("127.0.0.1/32")]
  [InlineData("127.0.0.0/24")]
  [InlineData("0.0.0.0/0")]
  public static void Ipv4PrefixToStringInvertible(string s)
  {
    var p = new Ipv4Prefix(s);
    Assert.Equal(s, p.ToString());
  }

  [Theory]
  [InlineData("127.0.0.0/24", "127.0.0.1/32")]
  [InlineData("70.0.19.0/24", "70.0.19.1")]
  [InlineData("70.0.19.0/24", "70.0.19.0")]
  public static void IpAddressRangeContainsIpv4Prefix(string range, string prefix)
  {
    var r = IPAddressRange.Parse(range);
    var p = new Ipv4Prefix(prefix);
    Assert.True(r.Contains(p));
  }

  [Theory]
  [InlineData("0.0.0.0", 0)]
  [InlineData("0.0.0.1", 1)]
  [InlineData("0.0.0.2", 2)]
  [InlineData("0.0.0.255", 8)]
  [InlineData("255.255.255.255", 32)]
  public static void IpAddressToPrefixLength(string addressString, uint expectedLength)
  {
    var address = IPAddress.Parse(addressString);
    var actualLength = address.ToUnsignedInt();
    Assert.Equal(expectedLength, actualLength);
  }
}

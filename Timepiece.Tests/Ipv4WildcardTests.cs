using System.Net;
using Timepiece.DataTypes;
using Xunit;

namespace Timepiece.Tests;

public static class Ipv4WildcardTests
{
  // [Theory]
  // [InlineData("127.0.0.1", "0.0.0.0")]
  // [InlineData("127.0.0.0", "0.0.0.255")]
  // public static void Ipv4WildcardToStringInvertible(string address, string mask)
  // {
  // var p = new Ipv4Wildcard(address, mask);
  // Assert.Equal(address, p.ToString());
  // }

  [Theory]
  [InlineData("127.0.0.0", "0.0.0.255", "127.0.0.1")]
  [InlineData("70.0.19.0", "0.0.0.1", "70.0.19.1")]
  [InlineData("70.0.19.0", "0.0.0.0", "70.0.19.0")]
  public static void Ipv4WildcardContainsIpv4Address(string address, string mask, string other)
  {
    var p = new Ipv4Wildcard(address, mask);
    Assert.True(p.ContainsIp(Ipv4Wildcard.AddressToUint(IPAddress.Parse(other))));
  }
}

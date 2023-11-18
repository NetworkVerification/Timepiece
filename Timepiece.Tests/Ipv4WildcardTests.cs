using System.Net;
using Timepiece.DataTypes;
using Xunit;

namespace Timepiece.Tests;

public class Ipv4WildcardTests
{
  [Theory]
  [InlineData("127.0.0.0", "0.0.0.255", "127.0.0.1", true)]
  [InlineData("70.0.19.0", "0.0.0.1", "70.0.19.1", true)]
  [InlineData("70.0.19.0", "0.0.0.0", "70.0.19.0", true)]
  [InlineData("0.0.0.0", "255.255.255.255", "0.0.0.0", true)]
  [InlineData("0.0.0.0", "255.255.255.255", "80.0.0.0", true)]
  [InlineData("0.0.0.0", "0.0.0.1", "0.0.0.1", true)]
  [InlineData("8.28.178.0", "0.0.1.255", "0.179.28.0", false)]
  public void Ipv4WildcardContainsIpv4Address(string prefix, string mask, string address, bool isContained)
  {
    var p = new Ipv4Wildcard(prefix, mask);
    Assert.Equal(isContained, p.ContainsIp(IPAddress.Parse(address).ToUnsignedInt()));
  }

  [Theory]
  [InlineData("127.0.0.0", "0.0.0.255", "127.0.0.0/24")]
  [InlineData("70.0.19.0", "0.0.0.1", "70.0.19.0/31")]
  [InlineData("70.0.19.0", "0.0.0.0", "70.0.19.0/32")]
  [InlineData("0.0.0.0", "255.255.255.255", "0.0.0.0/0")]
  [InlineData("0.0.0.0", "127.255.255.255", "0.0.0.0/1")]
  [InlineData("0.0.0.0", "0.0.0.1", "0.0.0.0/31")]
  [InlineData("1.2.3.4", "0.0.0.0", "1.2.3.4/32")]
  [InlineData("8.28.178.0", "0.0.1.255", "8.28.178.0/23")]
  public void Ipv4WildcardToPrefix(string wildcardPrefix, string wildcardMask, string prefix)
  {
    var p = new Ipv4Prefix(prefix);
    var w = new Ipv4Wildcard(wildcardPrefix, wildcardMask);
    Assert.Equal(p, w.ToPrefix());
  }

  [Theory]
  [InlineData("127.0.0.0", "0.0.0.255")]
  [InlineData("70.0.19.0", "0.0.0.1")]
  [InlineData("70.0.19.0", "0.0.0.0")]
  [InlineData("0.0.0.0", "255.255.255.255")]
  [InlineData("0.0.0.0", "127.255.255.255")]
  [InlineData("0.0.0.0", "0.0.0.1")]
  [InlineData("1.2.3.4", "0.0.0.0")]
  [InlineData("8.28.178.0", "0.0.1.255")]
  public void RoundTripViaPrefix(string address, string mask)
  {
    var w = new Ipv4Wildcard(address, mask);
    Assert.Equal(w, w.ToPrefix().ToWildcard());
  }
}

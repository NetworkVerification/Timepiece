using System.Linq;
using System.Net;
using NetTools;
using Newtonsoft.Json;
using ZenLib;

namespace Timekeeper.Datatypes;

/// <summary>
/// A Zen-friendly representation of an IPv4 prefix.
/// </summary>
public struct Ipv4Prefix
{
  public uint Prefix { get; set; }
  public UInt6 PrefixLength { get; set; }

  public Ipv4Prefix()
  {
    Prefix = 0;
    PrefixLength = new UInt6(32);
  }

  internal IPAddressRange AsAddressRange() => new(new IPAddress(Prefix), (int) PrefixLength.ToLong());

  /// <summary>
  /// Construct an IPv4 prefix from an address in CIDR notation.
  /// </summary>
  /// <param name="address">An IPv4 address in CIDR notation.</param>
  [JsonConstructor]
  public Ipv4Prefix(string address)
  {
    var range = IPAddressRange.Parse(address);
    // convert the address bytes back into a number
    // shift over by 8 bits each time
    Prefix = range.Begin.GetAddressBytes().Reverse().Aggregate(0U, (curr, b) => (curr << 8) | b);
    PrefixLength = new UInt6(range.GetPrefixLength());
  }

  public override string ToString()
  {
    return AsAddressRange().ToCidrString();
  }
}

public static class DestinationExt
{
  public static bool Contains(this IPAddressRange range, Ipv4Prefix d)
  {
    return range.Contains(d.AsAddressRange());
  }
}

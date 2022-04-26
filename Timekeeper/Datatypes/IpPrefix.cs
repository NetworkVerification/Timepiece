using System.Linq;
using System.Net;
using NetTools;
using ZenLib;

namespace Timekeeper.Datatypes;

/// <summary>
/// A Zen-friendly representation of an IPv4 prefix.
/// </summary>
public readonly struct IpPrefix
{
  public readonly uint prefix;
  public readonly UInt6 prefixLength = new(32);

  public IpPrefix()
  {
    prefix = 0;
  }

  internal IPAddressRange AsAddress() => new(new IPAddress(prefix), (int) prefixLength.ToLong());

  public IpPrefix(string address)
  {
    var ipAddress = IPAddress.Parse(address);
    // convert the address bytes back into a number
    // shift over by 8 bits each time
    prefix = ipAddress.GetAddressBytes().Aggregate(0U, (curr, b) => (curr << 8) | b);
  }

  public override string ToString()
  {
    return AsAddress().ToCidrString();
  }
}

public static class DestinationExt
{
  public static bool Contains(this IPAddressRange range, IpPrefix d)
  {
    return range.Contains(d.AsAddress());
  }
}

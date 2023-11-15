using System.Linq;
using System.Net;
using NetTools;
using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.DataTypes;

/// <summary>
///   A Zen-friendly representation of an IPv4 prefix.
/// </summary>
[ZenObject]
public struct Ipv4Prefix
{
  public uint Prefix { get; set; }

  public UInt<_6> PrefixLength { get; set; }

  public Ipv4Prefix()
  {
    Prefix = 0;
    PrefixLength = new UInt<_6>(32);
  }

  public Ipv4Prefix(IPAddressRange range)
  {
    Prefix = range.Begin.ToUnsignedInt();
    PrefixLength = new UInt<_6>(range.GetPrefixLength());
  }

  internal IPAddressRange AsAddressRange()
  {
    return new IPAddressRange(new IPAddress(Prefix), (int) PrefixLength.ToLong());
  }

  /// <summary>
  ///   Construct an IPv4 prefix from a prefix in CIDR notation.
  /// </summary>
  /// <param name="address">An IPv4 prefix in CIDR notation.</param>
  public Ipv4Prefix(string address) : this(IPAddressRange.Parse(address))
  {
  }

  public Ipv4Prefix(IPAddress begin, IPAddress end) : this(new IPAddressRange(begin, end))
  {
  }

  [JsonConstructor]
  public Ipv4Prefix(string begin, string end) : this(new IPAddressRange(IPAddress.Parse(begin), IPAddress.Parse(end)))
  {
  }

  public override string ToString()
  {
    // we use a try-catch here in case some aspect of the prefix is invalid; typically, it's the length
    try
    {
      return AsAddressRange().ToCidrString();
    }
    catch (System.FormatException)
    {
      return $"{Prefix}/{PrefixLength} (invalid)";
    }
  }
}

public static class Ipv4PrefixExtensions
{
  /// <summary>
  ///   Convert an IPv4 address to an unsigned integer by extracting the bytes.
  /// </summary>
  /// <param name="address"></param>
  /// <returns></returns>
  public static uint ToUnsignedInt(this IPAddress address) =>
    // we need to use Reverse() to flip the endianness of the bytes
    address.GetAddressBytes().Reverse().Aggregate(0U, (curr, b) => (curr << 8) | b);

  /// <summary>
  /// Verify that the given IPv4 prefix has a valid length (at most 32).
  /// </summary>
  /// <param name="prefix"></param>
  /// <returns></returns>
  public static Zen<bool> IsValidPrefixLength(this Zen<Ipv4Prefix> prefix)
    => prefix.GetPrefixLength() <= new UInt<_6>(32);

  public static bool Contains(this IPAddressRange range, Ipv4Prefix d)
  {
    return range.Contains(d.AsAddressRange());
  }
}

using System;
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
public struct Ipv4Prefix : IEquatable<Ipv4Prefix>
{
  public uint Prefix { get; set; }

  // TODO: constrain to at most 32
  public UInt<_6> PrefixLength { get; set; }

  public Ipv4Prefix()
  {
    Prefix = 0;
    PrefixLength = new UInt<_6>(32);
  }

  public Ipv4Prefix(IPAddressRange range)
  {
    Prefix = AddressToUint(range.Begin);
    PrefixLength = new UInt<_6>(range.GetPrefixLength());
  }

  internal IPAddressRange AsAddressRange()
  {
    return new IPAddressRange(new IPAddress(Prefix), (int) PrefixLength.ToLong());
  }

  /// <summary>
  ///   Convert an IPv4 address to an unsigned integer by extracting the bytes.
  /// </summary>
  /// <param name="address"></param>
  /// <returns></returns>
  private static uint AddressToUint(IPAddress address)
  {
    return address.GetAddressBytes().Reverse().Aggregate(0U, (curr, b) => (curr << 8) | b);
  }

  /// <summary>
  ///   Construct an IPv4 prefix from an address in CIDR notation.
  /// </summary>
  /// <param name="address">An IPv4 address in CIDR notation.</param>
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
    return AsAddressRange().ToCidrString();
    // return $"{Prefix}/{PrefixLength}";
  }

  public bool Equals(Ipv4Prefix other)
  {
    return Prefix == other.Prefix && Equals(PrefixLength, other.PrefixLength);
  }

  public override bool Equals(object obj)
  {
    return obj is Ipv4Prefix other && Equals(other);
  }

  public static bool operator ==(Ipv4Prefix left, Ipv4Prefix right)
  {
    return left.Equals(right);
  }

  public static bool operator !=(Ipv4Prefix left, Ipv4Prefix right)
  {
    return !left.Equals(right);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Prefix, PrefixLength);
  }
}

public static class Ipv4PrefixExtensions
{
  /// <summary>
  /// Check that the given IPv4 prefix has a valid length (at most 32).
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

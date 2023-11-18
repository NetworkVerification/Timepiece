using System;
using System.Linq;
using System.Net;
using NetTools;
using Newtonsoft.Json;
using ZenLib;
using Array = System.Array;

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

  public IPAddressRange AsAddressRange()
  {
    return new IPAddressRange(Prefix.ToIpAddress(), (int) PrefixLength.ToLong());
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

  public Ipv4Wildcard ToWildcard()
  {
    // convert the prefix length to a bit mask
    var bitMask = Bits.GetBitMask(4, (int) PrefixLength.ToLong());
    // reverse for endianness
    Array.Reverse(bitMask);
    return new Ipv4Wildcard(Prefix, ~BitConverter.ToUInt32(bitMask, 0));
  }

  public override string ToString()
  {
    // we use a try-catch here in case some aspect of the prefix is invalid; typically, it's the length
    try
    {
      return AsAddressRange().ToCidrString();
    }
    catch (FormatException)
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
  /// <param name="address">The IP address.</param>
  /// <returns>An unsigned integer.</returns>
  /// <remarks>
  ///   The endian-ness of IP addresses is big endian, but most systems use little endian for integers.
  ///   Hence, this method flips the endian-ness of the bytes according to the response of <see cref="BitConverter.IsLittleEndian"/>.
  /// </remarks>
  public static uint ToUnsignedInt(this IPAddress address) =>
    // we need to use Reverse() to flip the endian-ness of the bytes if the system is little-endian
    // as network addresses are always big-endian
    BitConverter.ToUInt32(
      (BitConverter.IsLittleEndian ? address.GetAddressBytes().Reverse() : address.GetAddressBytes()).ToArray(), 0);

  /// <summary>
  ///   Convert an unsigned integer to an IPv4 address.
  /// </summary>
  /// <param name="u">The unsigned integer.</param>
  /// <returns>An IPv4 address.</returns>
  /// <remarks>
  ///   The endian-ness of IP addresses is big endian, but most systems use little endian for integers.
  ///   Hence, this method flips the endian-ness of the bytes according to the response of <see cref="BitConverter.IsLittleEndian"/>.
  /// </remarks>
  public static IPAddress ToIpAddress(this uint u) =>
    new((BitConverter.IsLittleEndian ? BitConverter.GetBytes(u).Reverse() : BitConverter.GetBytes(u)).ToArray());

  /// <summary>
  /// Verify that the given IPv4 prefix has a valid length (at most 32).
  /// </summary>
  /// <param name="prefix"></param>
  /// <returns></returns>
  public static Zen<bool> IsValidPrefixLength(this Zen<Ipv4Prefix> prefix)
    => prefix.GetPrefixLength() <= new UInt<_6>(32);

  /// <summary>
  /// Return true if the IP address range contains the given IPv4 Prefix.
  /// </summary>
  /// <param name="range"></param>
  /// <param name="d"></param>
  /// <returns></returns>
  public static bool Contains(this IPAddressRange range, Ipv4Prefix d)
  {
    return range.Contains(d.AsAddressRange());
  }

  /// <summary>
  /// Encode that the given prefix matches the supplied Zen value prefix.
  /// Matching may be exact (at only this prefix length) or for any larger prefix length.
  /// </summary>
  /// <param name="prefix"></param>
  /// <param name="otherPrefix"></param>
  /// <param name="exact"></param>
  /// <returns></returns>
  public static Zen<bool> Matches(this Ipv4Prefix prefix, Zen<Ipv4Prefix> otherPrefix, bool exact)
  {
    return Zen.Constant(prefix.ToWildcard()).MatchesPrefix(otherPrefix, prefix.PrefixLength,
      exact ? prefix.PrefixLength : new UInt<_6>(32));
  }
}

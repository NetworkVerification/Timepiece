using System.Linq;
using System.Net;
using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.DataTypes;

/// <summary>
///   An IP address and a wildcard mask.
///   A wildcard mask is an inverted (sub)net mask: see https://en.wikipedia.org/wiki/Wildcard_mask.
///   Python's ipaddress module calls these "host masks".
/// </summary>
[ZenObject]
public class Ipv4Wildcard
{
  public Ipv4Wildcard(uint prefix, uint wildcardMask)
  {
    Prefix = prefix;
    WildcardMask = wildcardMask;
  }

  public Ipv4Wildcard(IPAddress prefix, IPAddress wildcardMask) : this(AddressToUint(prefix),
    AddressToUint(wildcardMask))
  {
  }

  [JsonConstructor]
  public Ipv4Wildcard(string begin, string hostMask) : this(IPAddress.Parse(begin), IPAddress.Parse(hostMask))
  {
  }


  /// <summary>
  ///   A 32-bit IPv4 address.
  /// </summary>
  public uint Prefix { get; set; }

  /// <summary>
  ///   A 32-bit address mask: set bits are "don't care" bits.
  ///   In other words:
  ///   0: the bit at this position must match
  ///   1: the bit at this position doesn't matter
  /// </summary>
  public uint WildcardMask { get; set; }

  /// <summary>
  ///   Convert an IPv4 address to an unsigned integer by extracting the bytes.
  /// </summary>
  /// <param name="address"></param>
  /// <returns></returns>
  public static uint AddressToUint(IPAddress address)
  {
    return address.GetAddressBytes().Reverse().Aggregate(0U, (curr, b) => (curr << 8) | b);
  }

  /// <summary>
  ///   Check that the given 32-bit address is contained in the given prefix.
  /// </summary>
  /// <param name="address"></param>
  /// <returns></returns>
  public bool ContainsIp(uint address)
  {
    // intuitively, to check that an address is contained by the prefix,
    // we need to simply see that the relevant unmasked portions of the address are equal to the masked prefix
    var maskedPrefix = Prefix | WildcardMask;
    var maskedAddress = address | WildcardMask;
    return maskedPrefix == maskedAddress;
  }
}

public static class Ipv4WildcardExtensions
{
  /// <summary>
  ///   Check that the given 32-bit address is contained in the given prefix.
  /// </summary>
  /// <param name="prefix"></param>
  /// <param name="address"></param>
  /// <returns></returns>
  public static Zen<bool> ContainsIp(this Zen<Ipv4Wildcard> prefix, Zen<uint> address)
  {
    var thisMasked = prefix.GetPrefix() | prefix.GetWildcardMask();
    var maskedAddress = address | prefix.GetWildcardMask();
    return thisMasked == maskedAddress;
  }

  public static Zen<bool> MatchesPrefix(this Zen<Ipv4Wildcard> wildcard, Zen<Ipv4Prefix> prefix, UInt<_6> minLength,
    UInt<_6> maxLength)
  {
    // the line matches the given prefix's address and the prefix length is within the allowed range
    return Zen.And(wildcard.ContainsIp(prefix.GetPrefix()), Zen.And(minLength <= prefix.GetPrefixLength(),
      prefix.GetPrefixLength() <= maxLength));
  }
}

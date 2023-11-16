using System.Net;
using NetTools;
using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.DataTypes;

/// <summary>
///   An IP address and a wildcard mask.
///   A wildcard mask is an inverted (sub)net mask: see https://en.wikipedia.org/wiki/Wildcard_mask.
///   Python's ipaddress module calls these "host masks".
/// </summary>
[ZenObject]
public struct Ipv4Wildcard
{
  public Ipv4Wildcard(uint prefix, uint wildcardMask)
  {
    Prefix = prefix;
    WildcardMask = wildcardMask;
  }

  public Ipv4Wildcard(IPAddress prefix, IPAddress wildcardMask) : this(prefix.ToUnsignedInt(),
    wildcardMask.ToUnsignedInt())
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
  ///   Verify that the given 32-bit address is contained in the given prefix.
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

  /// <summary>
  /// Return the corresponding Ipv4Prefix of the wildcard.
  /// </summary>
  /// <returns></returns>
  public Ipv4Prefix ToPrefix()
  {
    // we first do a bitwise NOT (~) on the wildcard mask to get the subnet mask, then convert
    // (IPAddressRange has a handy builtin we can use)
    var maskLength = IPAddressRange.SubnetMaskLength((~WildcardMask).ToIpAddress());
    var range = new IPAddressRange(Prefix.ToIpAddress(), maskLength: maskLength);
    return new Ipv4Prefix(range);
  }
}

public static class Ipv4WildcardExtensions
{
  /// <summary>
  ///   Verify that the given 32-bit address is contained in the given prefix.
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

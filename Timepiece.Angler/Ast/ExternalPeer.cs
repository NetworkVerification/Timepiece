using System.Net;
using Newtonsoft.Json;

namespace Timepiece.Angler.Ast;

/// <summary>
///   An external peer outside the network.
///   Identified via an IP address and its connections into the network.
/// </summary>
/// <param name="ip"></param>
/// <param name="peers"></param>
public record ExternalPeer(IPAddress ip, string[] peers)
{
  public readonly IPAddress ip = ip;
  [JsonProperty("Peering")] public readonly string[] peers = peers;

  [JsonConstructor]
  public ExternalPeer(string ip, string[] peers) : this(IPAddress.Parse(ip), peers)
  {
  }

  /// <summary>
  ///   Return the node's "name" -- its IP address as a CIDR string.
  /// </summary>
  public string Name => ip.ToString();
}

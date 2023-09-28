using System.Net;
using Newtonsoft.Json;

namespace Timepiece.Angler.Ast;

/// <summary>
/// An external peer outside the network.
/// Identified via an IP address, possibly an AS number, and its connections into the network.
/// </summary>
/// <param name="asNum"></param>
/// <param name="ip"></param>
/// <param name="peers"></param>
public record ExternalPeer(int? asNum, IPAddress ip, string[] peers)
{
  public readonly IPAddress ip = ip;
  public readonly int? asNum = asNum;
  [JsonProperty("Peering")]
  public readonly string[] peers = peers;

  [JsonConstructor]
  public ExternalPeer(int? asNum, string ip, string[] peers) : this(asNum, IPAddress.Parse(ip), peers)
  {
  }

  public string Name => asNum is not null ? asNum.Value.ToString() : ip.ToString();
}

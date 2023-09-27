using System.Net;
using Newtonsoft.Json;

namespace Timepiece.Angler.Ast;

/// <summary>
/// An external peer outside the network.
/// Identified via an IP address, and possibly also an AS number.
/// </summary>
/// <param name="asNum"></param>
/// <param name="ip"></param>
public record ExternalPeer(int? asNum, IPAddress ip)
{
  public readonly IPAddress ip = ip;
  public readonly int? asNum = asNum;

  [JsonConstructor]
  public ExternalPeer(int? asNum, string ip) : this(asNum, IPAddress.Parse(ip))
  {
  }

  public string Name => asNum is not null ? asNum.Value.ToString() : ip.ToString();
}

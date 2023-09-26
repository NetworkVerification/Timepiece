using System.Net;

namespace Timepiece.Angler.Ast;

public record ExternalPeer(int? asNum, IPAddress ip)
{
  public IPAddress ip = ip;
  public int? asNum = asNum;
}

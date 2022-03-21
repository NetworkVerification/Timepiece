using System.Net;

namespace Gardener;

/// <summary>
/// A routing destination
/// </summary>
public class Destination
{
  public IPAddress Address;

  public Destination(string address)
  {
    Address = IPAddress.Parse(address);
  }
}

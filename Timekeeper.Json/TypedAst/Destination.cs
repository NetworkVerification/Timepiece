using System.Net;
using Newtonsoft.Json;

namespace Timekeeper.Json.TypedAst;

/// <summary>
///   A routing destination
/// </summary>
public readonly struct Destination
{
  public readonly IPAddress address;

  [JsonConstructor]
  public Destination(string address)
  {
    this.address = IPAddress.Parse(address);
  }
}

using System.Text.Json.Serialization;

namespace Timepiece.Angler.UntypedAst;

/// <summary>
///   A collection of import and export policies for routing at a particular node.
/// </summary>
public readonly struct RoutingPolicies
{
  [JsonConstructor]
  public RoutingPolicies(int? asn, string import, string export)
  {
    Asn = asn;
    Import = import;
    Export = export;
  }

  public RoutingPolicies()
  {
    Asn = null;
    Import = null;
    Export = null;
  }

  public int? Asn { get; }

  public string? Import { get; }

  public string? Export { get; }
}

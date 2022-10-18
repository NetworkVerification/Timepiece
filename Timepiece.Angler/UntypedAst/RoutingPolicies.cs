using System.Text.Json.Serialization;

namespace Timepiece.Angler.UntypedAst;

/// <summary>
///   A collection of import and export policies for routing at a particular node.
/// </summary>
public readonly struct RoutingPolicies
{
  [JsonConstructor]
  public RoutingPolicies(string import, string export)
  {
    Import = import;
    Export = export;
  }

  public RoutingPolicies()
  {
    Import = null;
    Export = null;
  }

  public string? Import { get; }

  public string? Export { get; }
}

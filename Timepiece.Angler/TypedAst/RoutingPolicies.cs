using System.Text.Json.Serialization;

namespace Timepiece.Angler.TypedAst;

/// <summary>
///   A collection of import and export policies for routing at a particular node.
/// </summary>
public readonly struct RoutingPolicies
{
  [JsonConstructor]
  public RoutingPolicies(List<string> import, List<string> export)
  {
    Import = import;
    Export = export;
  }

  public RoutingPolicies()
  {
    Import = new List<string>();
    Export = new List<string>();
  }

  public List<string> Import { get; }

  public List<string> Export { get; }
}

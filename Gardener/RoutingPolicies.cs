using System.Text.Json.Serialization;

namespace Gardener;

/// <summary>
/// A collection of import and export policies for routing at a particular node.
/// </summary>
public class RoutingPolicies
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

  public List<string> Import { get; set; }

  public List<string> Export { get; set; }
}

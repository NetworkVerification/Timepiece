using System.Text.Json.Serialization;

namespace Gardener;

/// <summary>
/// A collection of import and export policies for routing at a particular node.
/// </summary>
/// <typeparam name="T">The type of routes.</typeparam>
public class RoutingPolicies<T>
{
  [JsonConstructor]
  public RoutingPolicies(List<string> import, List<string> export)
  {
    Import = import;
    Export = export;
  }

  public List<string> Import { get; set; }

  public List<string> Export { get; set; }
}

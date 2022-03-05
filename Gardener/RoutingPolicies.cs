using System.Text.Json.Serialization;

namespace Gardener;

/// <summary>
/// A collection of import and export policies for routing at a particular node.
/// </summary>
/// <typeparam name="T">The type of routes.</typeparam>
public class RoutingPolicies<T>
{
  [JsonConstructor]
  public RoutingPolicies(List<AstFunc<T, T>> import, List<AstFunc<T, T>> export)
  {
    Import = import;
    Export = export;
  }

  public List<AstFunc<T, T>> Import { get; set; }

  public List<AstFunc<T, T>> Export { get; set; }
}

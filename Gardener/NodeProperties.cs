namespace Gardener;

/// <summary>
/// Representation of the properties of a node as parsed from JSON.
/// Tracks the node's prefixes and its routing policies.
/// </summary>
/// <typeparam name="T">The type of routes in the node's RoutingPolicies.</typeparam>
public class NodeProperties<T>
{
  public NodeProperties(List<string> prefixes, Dictionary<string, RoutingPolicies<T>> policies)
  {
    Prefixes = prefixes;
    Policies = policies;
  }

  public List<string> Prefixes { get; }

  public Dictionary<string, RoutingPolicies<T>> Policies { get; }
}

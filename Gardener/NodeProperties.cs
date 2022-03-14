using System.Numerics;
using Gardener.AstExpr;
using NetTools;
using ZenLib;

namespace Gardener;

/// <summary>
/// Representation of the properties of a node as parsed from JSON.
/// Tracks the node's prefixes and its routing policies.
/// </summary>
/// <typeparam name="T">The type of routes in the node's RoutingPolicies.</typeparam>
public class NodeProperties<T>
{
  public NodeProperties(List<IPAddressRange> prefixes, Dictionary<string, RoutingPolicies<T>> policies,
    string? assert, string? invariant)
  {
    Prefixes = prefixes;
    Policies = policies;
    Assert = assert;
    Invariant = invariant;
  }

  public string? Invariant { get; set; }

  public List<IPAddressRange> Prefixes { get; }

  public Dictionary<string, RoutingPolicies<T>> Policies { get; }

  public string? Assert { get; }
}

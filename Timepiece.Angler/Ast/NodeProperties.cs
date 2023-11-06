using System.Collections.Immutable;
using Newtonsoft.Json;
using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
///   Representation of the properties of a node as parsed from JSON.
///   Tracks the node's prefixes and its routing policies.
/// </summary>
public class NodeProperties
{
  [JsonConstructor]
  public NodeProperties(int? asn, Dictionary<string, RoutingPolicies> policies,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations,
    List<Ipv4Prefix> prefixes)
  {
    Asn = asn;
    Policies = policies;
    Prefixes = prefixes;
    Declarations = declarations;
  }

  /// <summary>
  ///   Construct a new <c>NodeProperties</c> instance with the given neighbors
  ///   and no policy or ASN information.
  /// </summary>
  /// <param name="neighbors"></param>
  public NodeProperties(IEnumerable<string> neighbors) : this(null,
    neighbors.ToDictionary(n => n, _ => new RoutingPolicies()),
    new Dictionary<string, AstFunction<RouteEnvironment>>(), new List<Ipv4Prefix>())
  {
  }

  /// <summary>
  ///   The prefixes associated with the node. Not currently used.
  /// </summary>
  public List<Ipv4Prefix> Prefixes { get; set; }

  /// <summary>
  ///   AS number for the given node.
  /// </summary>
  [JsonProperty("ASNumber")]
  public int? Asn { get; set; }

  /// <summary>
  ///   Additional function declarations.
  /// </summary>
  [JsonProperty(nameof(Declarations))]
  public Dictionary<string, AstFunction<RouteEnvironment>> Declarations { get; set; }

  [JsonProperty(nameof(Policies))] public Dictionary<string, RoutingPolicies> Policies { get; }

  /// <summary>
  ///   Construct a <c>NetworkNode{RouteEnvironment}</c> instance that stores the
  ///   transfer functions used for each incoming and outgoing peer.
  ///   If a peer has no associated behavior, use the given default export and import functions.
  /// </summary>
  /// <param name="defaultExport">A default export function.</param>
  /// <param name="defaultImport">A default import function.</param>
  /// <param name="trackTerms">Whether or not to track the visited terms.</param>
  /// <returns></returns>
  public NetworkNode<RouteEnvironment> CreateNode(Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> defaultExport,
    Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>> defaultImport, bool trackTerms)
  {
    var env = new AstState(Declarations) {TrackTerms = trackTerms};

    var imports = new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    var exports = new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    foreach (var (neighbor, policies) in Policies)
    {
      if (policies.Export is null)
        exports[neighbor] = defaultExport;
      else
        exports[neighbor] = env.EvaluateFunction(Declarations[policies.Export]);

      if (policies.Import is null)
        imports[neighbor] = defaultImport;
      else
        imports[neighbor] = env.EvaluateFunction(Declarations[policies.Import]);
    }

    return new NetworkNode<RouteEnvironment>(imports.ToImmutableDictionary(), exports.ToImmutableDictionary());
  }
}

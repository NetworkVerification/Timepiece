using System.Collections.Immutable;
using Newtonsoft.Json;
using Timepiece.Angler.Ast.AstFunction;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
///   Representation of the properties of a node as parsed from JSON.
///   Tracks the node's prefixes and its routing policies.
/// </summary>
public class NodeProperties
{
  public NodeProperties(int? asn, Dictionary<string, RoutingPolicies> policies,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations,
    List<Ipv4Prefix> prefixes)
  {
    Asn = asn;
    Policies = policies;
    Prefixes = prefixes;
    Declarations = declarations;
    // DisambiguateVariableNames();
  }

  public List<Ipv4Prefix> Prefixes { get; set; }

  /// <summary>
  ///   AS number for the given node.
  /// </summary>
  public int? Asn { get; set; }


  /// <summary>
  ///   Additional function declarations.
  /// </summary>
  [JsonProperty(nameof(Declarations))]
  public Dictionary<string, AstFunction<RouteEnvironment>> Declarations { get; set; }

  [JsonProperty(nameof(Policies))] public Dictionary<string, RoutingPolicies> Policies { get; }

  /// <summary>
  ///   Make the arguments to all AstFunctions unique.
  /// </summary>
  private void DisambiguateVariableNames()
  {
    foreach (var function in Declarations.Values)
    {
      function.Rename(function.Arg, $"${function.Arg}~{VarCounter.Request()}");
      Console.WriteLine($"New function arg: {function.Arg}");
    }
  }

  /// <summary>
  ///   Return true if the given neighbor is considered to not be in the same AS as this node.
  /// </summary>
  /// <param name="neighbor"></param>
  /// <returns></returns>
  public bool IsExternalNeighbor(string neighbor)
  {
    return Asn is null || Asn != Policies[neighbor].Asn;
  }

  /// <summary>
  ///   Construct a node storing all the relevant information for creating a network.
  /// </summary>
  /// <param name="defaultExport">A default export function.</param>
  /// <param name="defaultImport">A default import function.</param>
  /// <returns></returns>
  public NetworkNode<RouteEnvironment> CreateNode(AstFunction<RouteEnvironment> defaultExport,
    AstFunction<RouteEnvironment> defaultImport)
  {
    var env = new AstEnvironment(Declarations);
    // add the symbolic values to the environment

    var imports = new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    var exports = new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    foreach (var (neighbor, policies) in Policies)
    {
      if (policies.Export is null)
        exports[neighbor] = env.EvaluateFunction(defaultExport);
      else
        exports[neighbor] = env.EvaluateFunction(Declarations[policies.Export]);

      if (policies.Import is null)
        imports[neighbor] = env.EvaluateFunction(defaultImport);
      else
        imports[neighbor] = env.EvaluateFunction(Declarations[policies.Import]);
    }

    return new NetworkNode<RouteEnvironment>(imports.ToImmutableDictionary(), exports.ToImmutableDictionary());
  }
}

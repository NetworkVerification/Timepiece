using System.Collections.Immutable;
using Newtonsoft.Json;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

/// <summary>
///   Representation of the properties of a node as parsed from JSON.
///   Tracks the node's prefixes and its routing policies.
/// </summary>
public class NodeProperties
{
  public NodeProperties(int? asn, Dictionary<string, RoutingPolicies> policies,
    string? stable, AstTemporalOperator? temporal, Dictionary<string, AstFunction<RouteEnvironment>> declarations,
    Expr initial)
  {
    Asn = asn;
    Policies = policies;
    Stable = stable;
    Temporal = temporal;
    Initial = initial;
    Declarations = declarations;
    // DisambiguateVariableNames();
  }

  /// <summary>
  ///   AS number for the given node.
  /// </summary>
  public int? Asn { get; set; }

  private Expr Initial { get; }

  /// <summary>
  ///   Additional function declarations.
  /// </summary>
  [JsonProperty("Declarations")]
  public Dictionary<string, AstFunction<RouteEnvironment>> Declarations { get; set; }

  public AstTemporalOperator? Temporal { get; set; }

  [JsonProperty("Policies")] public Dictionary<string, RoutingPolicies> Policies { get; }

  //[JsonProperty(Required = Required.DisallowNull)]
  public string? Stable { get; }

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
  /// <param name="predicateLookupFunction">A function that returns a predicate given its string name.</param>
  /// <param name="defaultExport">A default export function.</param>
  /// <param name="defaultImport">A default import function.</param>
  /// <param name="symbolicValues">A sequence of symbolic values.</param>
  /// <returns></returns>
  public NetworkNode<RouteEnvironment> CreateNode(
    Func<string, AstPredicate> predicateLookupFunction, AstFunction<RouteEnvironment> defaultExport,
    AstFunction<RouteEnvironment> defaultImport, IEnumerable<SymbolicValue<RouteEnvironment>> symbolicValues)
  {
    var env = new AstEnvironment(Declarations);
    // add the symbolic values to the environment
    env = symbolicValues.Aggregate(env,
      (current, symbolicValue) => current.Update(symbolicValue.Name, symbolicValue.Value));

    var init = env
      .EvaluateExpr(
        new Environment<RouteEnvironment>(Zen.Symbolic<RouteEnvironment>()),
        Initial).returnValue;

    var safetyProperty = Stable is null
      ? _ => true
      : predicateLookupFunction(Stable).Evaluate(env);

    var invariant = Temporal is null
      ? (_, _) => true
      : Temporal.Evaluate(predicateLookupFunction, Declarations);

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

    return new NetworkNode<RouteEnvironment>(init, safetyProperty, invariant, imports.ToImmutableDictionary(),
      exports.ToImmutableDictionary());
  }
}

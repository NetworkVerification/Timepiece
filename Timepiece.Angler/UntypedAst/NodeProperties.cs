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
  public NodeProperties(Dictionary<string, RoutingPolicies> policies,
    string? stable, AstTemporalOperator? temporal, Dictionary<string, AstFunction<RouteEnvironment>> declarations,
    Expr initial)
  {
    Policies = policies;
    Stable = stable;
    Temporal = temporal;
    Initial = initial;
    Declarations = declarations;
    DisambiguateVariableNames();
  }

  private Expr Initial { get; set; }

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
  ///   Construct a node storing all the relevant information for creating a network.
  /// </summary>
  /// <param name="predicateLookupFunction"></param>
  /// <param name="defaultExport"></param>
  /// <param name="defaultImport"></param>
  /// <returns></returns>
  public NetworkNode<RouteEnvironment> CreateNode(
    Func<string, AstPredicate> predicateLookupFunction, AstFunction<RouteEnvironment> defaultExport,
    AstFunction<RouteEnvironment> defaultImport)
  {
    var env = new AstEnvironment(Declarations);

    var init = env.EvaluateExpr(Zen.Symbolic<RouteEnvironment>(), Initial).Item2;

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
      {
        exports[neighbor] = env.EvaluateFunction(defaultExport);
      }
      else
      {
        exports[neighbor] = env.EvaluateFunction(Declarations[policies.Export]);
      }

      if (policies.Import is null)
      {
        imports[neighbor] = env.EvaluateFunction(defaultImport);
      }
      else
      {
        imports[neighbor] = env.EvaluateFunction(Declarations[policies.Import]);
      }
    }

    return new NetworkNode<RouteEnvironment>(init, safetyProperty, invariant, imports.ToImmutableDictionary(),
      exports.ToImmutableDictionary());
  }
}

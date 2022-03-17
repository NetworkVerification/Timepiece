using System.Collections.Immutable;
using System.Numerics;
using Gardener.AstFunction;
using NetTools;
using Newtonsoft.Json.Linq;
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
    string? assert, string? invariant, Dictionary<string, AstFunction<T>> declarations,
    Dictionary<string, JObject> constants)
  {
    Prefixes = prefixes;
    Policies = policies;
    Assert = assert;
    Invariant = invariant;
    Declarations = declarations;
    Constants = constants;
    DisambiguateVariableNames();
  }

  /// <summary>
  /// Additional function declarations.
  /// </summary>
  public Dictionary<string, AstFunction<T>> Declarations { get; set; }

  /// <summary>
  /// Additional constant declarations.
  /// </summary>
  public Dictionary<string, JObject> Constants { get; set; }

  public string? Invariant { get; set; }

  public List<IPAddressRange> Prefixes { get; }

  public Dictionary<string, RoutingPolicies<T>> Policies { get; }

  public string? Assert { get; }

  /// <summary>
  /// Make the arguments to all AstFunctions unique.
  /// </summary>
  private void DisambiguateVariableNames()
  {
    foreach (var function in Declarations.Values)
    {
      function.Rename(function.Arg, $"${function.Arg}~{VarCounter.Request()}");
      Console.WriteLine($"New function arg: {function.Arg}");
    }
  }

  public KaresansuiNode<T> CreateNode(Func<List<IPAddressRange>, Zen<T>> initFunction,
    Func<string, AstPredicate<Pair<T, BigInteger>>> assertFunction,
    Func<string, AstPredicate<Pair<T, BigInteger>>> invariantFunction,
    AstFunction<T> defaultExport,
    AstFunction<T> defaultImport)
  {
    var init = initFunction(Prefixes);

    var safetyProperty = Assert is null
      ? (_, _) => true
      : assertFunction(Assert).EvaluateBinary(new State<Pair<T, BigInteger>>());

    var invariant = Invariant is null
      ? (_, _) => true
      : invariantFunction(Invariant).EvaluateBinary(new State<Pair<T, BigInteger>>());

    var imports = new Dictionary<string, Func<Zen<T>, Zen<T>>>();
    var exports = new Dictionary<string, Func<Zen<T>, Zen<T>>>();
    foreach (var (neighbor, policies) in Policies)
    {
      var exportAstFunctions = policies.Export.Select(policyName => Declarations[policyName]);
      var importAstFunctions = policies.Import.Select(policyName => Declarations[policyName]);
      exports[neighbor] = AstFunction<T>.Compose(exportAstFunctions, defaultExport).Evaluate(new State<T>());
      imports[neighbor] = AstFunction<T>.Compose(importAstFunctions, defaultImport).Evaluate(new State<T>());
    }

    return new KaresansuiNode<T>(init, safetyProperty, invariant, imports.ToImmutableDictionary(),
      exports.ToImmutableDictionary());
  }
}

using System.Numerics;
using Gardener.AstFunction;
using Karesansui;
using Karesansui.Networks;
using NetTools;
using ZenLib;

namespace Gardener;

public class Ast<T, TS>
{
  /// <summary>
  /// The nodes of the network with their associated policies.
  /// </summary>
  public Dictionary<string, NodeProperties<T>> Nodes { get; }

  /// <summary>
  /// Symbolic expressions.
  /// </summary>
  public Dictionary<string, AstPredicate<TS>> Symbolics { get; }

  /// <summary>
  /// Assertions over routes.
  /// </summary>
  public Dictionary<string, AstPredicate<Pair<T, BigInteger>>> Assertions { get; }


  /// <summary>
  /// Temporal invariants over routes.
  /// </summary>
  public Dictionary<string, AstPredicate<Pair<T, BigInteger>>> Invariants { get; }

  [System.Text.Json.Serialization.JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<T>> nodes,
    Dictionary<string, AstPredicate<TS>> symbolics,
    Dictionary<string, AstPredicate<Pair<T, BigInteger>>> assertions,
    Dictionary<string, AstPredicate<Pair<T, BigInteger>>> invariants)
  {
    Nodes = nodes;
    Symbolics = symbolics;
    Assertions = assertions;
    Invariants = invariants;
  }

  public Network<T, TS> ToNetwork(Func<List<IPAddressRange>, Zen<T>> initGenerator,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction, AstFunction<T> defaultExport,
    AstFunction<T> defaultImport)
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, List<string>>();
    var initFunction = new Dictionary<string, Zen<T>>();
    var modularProperties = new Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>();
    var annotations = new Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>();
    var exportFunctions = new Dictionary<(string, string), Func<Zen<T>, Zen<T>>>();
    var importFunctions = new Dictionary<(string, string), Func<Zen<T>, Zen<T>>>();

    foreach (var (node, props) in Nodes)
    {
      var details = props.CreateNode(initGenerator, s => Assertions[s], s => Invariants[s],
        defaultExport, defaultImport);
      edges[node] = details.imports.Keys.Union(details.exports.Keys).ToList();
      initFunction[node] = details.initialValue;
      modularProperties[node] = details.safetyProperty;
      annotations[node] = details.annotation;
      foreach (var (nbr, fn) in details.exports)
      {
        exportFunctions[(node, nbr)] = fn;
      }

      foreach (var (nbr, fn) in details.imports)
      {
        importFunctions[(nbr, node)] = fn;
      }
    }

    var transferFunction = new Dictionary<(string, string), Func<Zen<T>, Zen<T>>>();
    foreach (var (edge, export) in exportFunctions)
    {
      // compose the export and import and evaluate on a fresh state
      // NOTE: assumes that every export edge has a corresponding import edge (i.e. the graph is undirected)
      transferFunction.Add(edge, r => importFunctions[edge](export(r)));
    }

    var topology = new Topology(edges);
    // construct a reasonable estimate of the monolithic properties by checking that the modular properties
    // will hold at a time equal to the number of nodes in the network (i.e. the longest path possible)
    var monolithicProperties =
      topology.ForAllNodes<Func<Zen<T>, Zen<bool>>>(n => r => modularProperties[n](r, new BigInteger(topology.NEdges)));

    return new Network<T, TS>(topology,
      transferFunction,
      mergeFunction,
      initFunction,
      annotations,
      modularProperties,
      monolithicProperties,
      Symbolics.Select(nameConstraint =>
        new SymbolicValue<TS>(nameConstraint.Key, nameConstraint.Value.Evaluate(new State<TS>()))).ToArray());
  }
}

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
  /// An optional routing destination prefix.
  /// </summary>
  public Destination? Destination { get; }

  /// <summary>
  /// The nodes of the network with their associated policies.
  /// </summary>
  public Dictionary<string, NodeProperties<T>> Nodes { get; }

  /// <summary>
  /// Symbolic expressions.
  /// </summary>
  public Dictionary<string, AstPredicate<TS>> Symbolics { get; }

  /// <summary>
  /// Assertions over routes, irrespective of time.
  /// </summary>
  public Dictionary<string, AstPredicate<T>> Assertions { get; }


  [System.Text.Json.Serialization.JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<T>> nodes,
    Dictionary<string, AstPredicate<TS>> symbolics,
    Dictionary<string, AstPredicate<T>> assertions, Destination? destination)
  {
    Nodes = nodes;
    Symbolics = symbolics;
    Assertions = assertions;
    Destination = destination;
  }

  public Network<T, TS> ToNetwork(Func<bool, Zen<T>> initGenerator,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction, AstFunction<T> defaultExport,
    AstFunction<T> defaultImport)
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, List<string>>();
    var initFunction = new Dictionary<string, Zen<T>>();
    var monolithicProperties = new Dictionary<string, Func<Zen<T>, Zen<bool>>>();
    var annotations = new Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>();
    var exportFunctions = new Dictionary<(string, string), Func<Zen<T>, Zen<T>>>();
    var importFunctions = new Dictionary<(string, string), Func<Zen<T>, Zen<T>>>();

    var isDestination = new Func<List<IPAddressRange>, bool>(prefixes =>
    {
      return Destination is not null && prefixes.Any(p => p.Contains(Destination.Address));
    });
    // using Evaluate() to convert AST elements into functions over Zen values is likely to be a bit slow
    // we hence want to try and do as much of this as possible up front
    // this also means inlining constants and evaluating and inlining assertions where possible
    foreach (var (node, props) in Nodes)
    {
      var details = props.CreateNode(p => initGenerator(isDestination(p)), s => Assertions[s],
        defaultExport, defaultImport);
      edges[node] = details.imports.Keys.Union(details.exports.Keys).ToList();
      monolithicProperties[node] = details.safetyProperty;
      initFunction[node] = details.initialValue;
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
    // construct a reasonable estimate of the modular properties by checking that the monolithic properties
    // will eventually hold at a time equal to the number of nodes in the network (i.e. the longest path possible)
    var modularProperties =
      topology.ForAllNodes<Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>(n =>
        Lang.Finally(new BigInteger(topology.NEdges), monolithicProperties[n]));

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

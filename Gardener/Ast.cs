using System.Numerics;
using Gardener.AstFunction;
using Karesansui;
using Karesansui.Networks;
using NetTools;
using Newtonsoft.Json;
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
  /// Predicates over routes, irrespective of time.
  /// </summary>
  public Dictionary<string, AstPredicate<T>> Predicates { get; }

  /// <summary>
  /// An optional convergence time.
  /// </summary>
  public BigInteger? ConvergeTime { get; set; }

  [JsonConstructor]
  public Ast(Dictionary<string, NodeProperties<T>> nodes,
    Dictionary<string, AstPredicate<TS>> symbolics,
    Dictionary<string, AstPredicate<T>> predicates, Destination? destination, BigInteger? convergeTime)
  {
    Nodes = nodes;
    Symbolics = symbolics;
    Predicates = predicates;
    Destination = destination;
    ConvergeTime = convergeTime;
  }

  /// <summary>
  /// Print validation stats for the AST, including warnings if variables are uninitialized.
  /// </summary>
  public void Validate()
  {
    if (Destination is null)
    {
      WarnLine("No destination given.");
    }
    else
    {
      Console.WriteLine($"Destination: {Destination.Address}");
    }

    if (ConvergeTime is null)
    {
      WarnLine("No convergence time given.");
    }
    else
    {
      Console.WriteLine($"Converge time: {ConvergeTime}");
    }

    foreach (var (node, props) in Nodes)
    {
      Console.Write($"Node {node}: ");
      if (props.Stable is null)
      {
        Warn("no assert found");
      }
      else
      {
        Console.Write($"assert {props.Stable}");
      }

      if (props.Temporal is null)
      {
        WarnLine(" no invariant found");
      }
      else
      {
        Console.WriteLine($" invariant {props.Temporal}");
      }
    }

    Console.Write("Predicates defined:");
    foreach (var predicateName in Predicates.Keys)
    {
      Console.Write($" {predicateName};");
    }

    Console.WriteLine();

    Console.Write("Symbolics defined:");
    foreach (var symbolicName in Symbolics.Keys)
    {
      Console.Write($" {symbolicName};");
    }

    Console.WriteLine();
  }

  private static void Warn(string s)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(s);
    Console.ResetColor();
  }

  private static void WarnLine(string s)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(s);
    Console.ResetColor();
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
    // this also means inlining constants and evaluating and inlining predicates where possible
    foreach (var (node, props) in Nodes)
    {
      var details = props.CreateNode(p => initGenerator(isDestination(p)),
        s => Predicates[s],
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
    var convergeTime = ConvergeTime ?? new BigInteger(topology.NEdges);
    var modularProperties =
      topology.ForAllNodes<Func<Zen<T>, Zen<BigInteger>, Zen<bool>>>(n =>
        Lang.Finally(convergeTime, monolithicProperties[n]));

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

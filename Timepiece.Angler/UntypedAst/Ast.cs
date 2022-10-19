using System.Numerics;
using Newtonsoft.Json;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Datatypes;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler.UntypedAst;

public class Ast
{
  [JsonConstructor]
  public Ast(Dictionary<string, NodeProperties> nodes,
    Dictionary<string, AstPredicate> symbolics,
    Dictionary<string, AstPredicate> predicates, Ipv4Prefix? destination, BigInteger? convergeTime)
  {
    Nodes = nodes;
    Symbolics = symbolics;
    Predicates = predicates;
    Destination = destination;
    ConvergeTime = convergeTime;
  }

  /// <summary>
  ///   An optional routing destination prefix.
  /// </summary>
  public Ipv4Prefix? Destination { get; }

  /// <summary>
  ///   The nodes of the network with their associated policies.
  /// </summary>
  [JsonRequired]
  public Dictionary<string, NodeProperties> Nodes { get; }

  /// <summary>
  ///   Symbolic expressions.
  /// </summary>
  public Dictionary<string, AstPredicate> Symbolics { get; }

  /// <summary>
  ///   Predicates over routes, irrespective of time.
  /// </summary>
  public Dictionary<string, AstPredicate> Predicates { get; }

  /// <summary>
  ///   An optional convergence time.
  /// </summary>
  public BigInteger? ConvergeTime { get; set; }

  /// <summary>
  ///   Print validation stats for the AST, including warnings if variables are uninitialized.
  /// </summary>
  public void Validate()
  {
    if (Destination.HasValue)
      Console.WriteLine($"Destination: {Destination.Value}");
    else
      WarnLine("No destination given.");

    if (ConvergeTime is null)
      WarnLine("No convergence time given.");
    else
      Console.WriteLine($"Converge time: {ConvergeTime}");

    foreach (var (node, props) in Nodes)
    {
      Console.Write($"Node {node}: ");
      if (props.Stable is null)
        Warn("no assert found");
      else
        Console.Write($"assert {props.Stable}");

      if (props.Temporal is null)
        WarnLine(" no invariant found");
      else
        Console.WriteLine($" invariant {props.Temporal}");
    }

    Console.Write("Predicates defined:");
    foreach (var predicateName in Predicates.Keys) Console.Write($" {predicateName};");

    Console.WriteLine();

    Console.Write("Symbolics defined:");
    foreach (var symbolicName in Symbolics.Keys) Console.Write($" {symbolicName};");

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

  public Network<RouteEnvironment, RouteEnvironment> ToNetwork(
    Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>, Zen<RouteEnvironment>> mergeFunction,
    AstFunction<RouteEnvironment> defaultExport, AstFunction<RouteEnvironment> defaultImport)
  {
    // construct all the mappings we'll need
    var edges = new Dictionary<string, List<string>>();
    var initFunction = new Dictionary<string, Zen<RouteEnvironment>>();
    var monolithicProperties = new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<bool>>>();
    var annotations = new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>>>();
    var exportFunctions = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    var importFunctions = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();

    // using Evaluate() to convert AST elements into functions over Zen values is likely to be a bit slow
    // we hence want to try and do as much of this as possible up front
    // this also means inlining constants and evaluating and inlining predicates where possible
    foreach (var (node, props) in Nodes)
    {
      var details = props.CreateNode(
        s => Predicates.ContainsKey(s) ? Predicates[s] : throw new ArgumentException("Predicate {s} not found!"),
        defaultExport, defaultImport);
      edges[node] = details.imports.Keys.Union(details.exports.Keys).ToList();
      monolithicProperties[node] = details.safetyProperty;
      initFunction[node] = details.initialValue;
      annotations[node] = details.annotation;
      foreach (var (nbr, fn) in details.exports) exportFunctions[(node, nbr)] = fn;

      foreach (var (nbr, fn) in details.imports) importFunctions[(nbr, node)] = fn;
    }

    var transferFunction = new Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>>();
    foreach (var (edge, export) in exportFunctions)
      // compose the export and import and evaluate on a fresh state
      // NOTE: assumes that every export edge has a corresponding import edge (i.e. the graph is undirected)
      transferFunction.Add(edge, r => importFunctions[edge](export(r)));

    var topology = new Topology(edges);
    // construct a reasonable estimate of the modular properties by checking that the monolithic properties
    // will eventually hold at a time equal to the number of nodes in the network (i.e. the longest path possible)
    var convergeTime = ConvergeTime ?? new BigInteger(topology.NEdges);
    var modularProperties =
      topology.MapNodes<Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>>>(n =>
        Lang.Finally(convergeTime, monolithicProperties[n]));

    return new Network<RouteEnvironment, RouteEnvironment>(topology,
      transferFunction,
      mergeFunction,
      initFunction,
      annotations,
      modularProperties,
      monolithicProperties,
      Symbolics.Select(nameConstraint =>
          new SymbolicValue<RouteEnvironment>(nameConstraint.Key, nameConstraint.Value.Evaluate(new AstEnvironment())))
        .ToArray());
  }
}

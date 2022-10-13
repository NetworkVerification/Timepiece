using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.Datatypes;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

/// <summary>
/// Represents an NV network.
/// </summary>
/// <typeparam name="T">The type of the routes.</typeparam>
/// <typeparam name="TS">The type of symbolic values associated with the network.</typeparam>
public class Network<T, TS>
{
  /// <summary>
  /// The initial values for each node.
  /// </summary>
  protected Dictionary<string, Zen<T>> InitialValues { get; init; }

  /// <summary>
  /// The merge function for routes.
  /// </summary>
  public Func<Zen<T>, Zen<T>, Zen<T>> MergeFunction { get; }

  /// <summary>
  /// The modular safety properties that we want to check (includes time).
  /// </summary>
  public Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> ModularProperties { get; set; }

  /// <summary>
  /// The monolithic safety properties that we want to check (assumes stable states).
  /// </summary>
  public Dictionary<string, Func<Zen<T>, Zen<bool>>> MonolithicProperties { get; set; }

  /// <summary>
  /// Any additional symbolics on the network's components.
  /// </summary>
  public SymbolicValue<TS>[] Symbolics { get; set; }

  /// <summary>
  /// The topology of the network.
  /// </summary>
  public Topology Topology { get; }

  /// <summary>
  /// The transfer function for each edge.
  /// </summary>
  public Dictionary<(string, string), Func<Zen<T>, Zen<T>>> TransferFunction { get; protected init; }

  /// <summary>
  /// The invariant/annotation function for each node. Takes a route and a time and returns a boolean.
  /// </summary>
  protected Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> Annotations { get; init; }

  /// <summary>
  /// Construct a new Network.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="transferFunction"></param>
  /// <param name="mergeFunction"></param>
  /// <param name="initialValues"></param>
  /// <param name="annotations"></param>
  /// <param name="modularProperties"></param>
  /// <param name="monolithicProperties"></param>
  /// <param name="symbolics"></param>
  public Network(
    Topology topology,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<string, Zen<T>> initialValues,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<T>, Zen<bool>>> monolithicProperties,
    SymbolicValue<TS>[] symbolics)
  {
    Topology = topology;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitialValues = initialValues;
    Symbolics = symbolics;
    Annotations = annotations;
    ModularProperties = modularProperties;
    MonolithicProperties = monolithicProperties;
  }

  /// <summary>
  /// Construct a new Network using an alternate properties definition.
  /// For modular checking, for each node we check a property
  /// Globally(safetyProperties[n]) and Finally(convergeTime, stableProperties[n]).
  /// For monolithic checking, for each node we check a property
  /// safetyProperties[n] and stableProperties[n].
  /// </summary>
  /// <param name="topology">The network topology.</param>
  /// <param name="transferFunction"></param>
  /// <param name="mergeFunction"></param>
  /// <param name="initialValues"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties">Properties which are true at convergence.</param>
  /// <param name="safetyProperties">Properties which are always true (in the modular network).</param>
  /// <param name="convergeTime">The convergence time of the network.</param>
  /// <param name="symbolics"></param>
  public Network(Topology topology,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<string, Zen<T>> initialValues,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime,
    SymbolicValue<TS>[] symbolics) : this(topology,
    transferFunction, mergeFunction, initialValues, annotations,
    // modular properties: Finally(stable) + Globally(safety)
    topology.MapNodes(n =>
      Lang.Intersect(Lang.Finally(convergeTime, stableProperties[n]), Lang.Globally(safetyProperties[n]))),
    // monolithic properties: stable + safety
    topology.MapNodes(n => Lang.Intersect(stableProperties[n], safetyProperties[n])),
    symbolics)
  {
  }

  /// <summary>
  /// Check that the annotations are sound, calling the given function f on each node's check.
  /// </summary>
  /// <param name="collector"></param>
  /// <param name="f"></param>
  /// <returns></returns>
  public Option<State<T, TS>> CheckAnnotationsWith<TAcc>(TAcc collector,
    Func<string, TAcc, Func<Option<State<T, TS>>>, Option<State<T, TS>>> f)
  {
    var routes = Topology.MapNodes(_ => Symbolic<T>());
    var time = Symbolic<BigInteger>();
    var s = Topology.Nodes
      // call f for each node
      .AsParallel()
      .Select(node => f(node, collector, () => CheckAnnotations(node, routes, time)))
      .FirstOrDefault(s => s.HasValue, Option.None<State<T, TS>>());
    return s;
  }

  public Option<State<T, TS>> CheckAnnotations(string node, IReadOnlyDictionary<string, Zen<T>> routes,
    Zen<BigInteger> time)
  {
    return CheckBaseCase(node).OrElse(() => CheckInductive(node, routes, time)).OrElse(() => CheckAssertions(node));
  }

  /// <summary>
  /// Check that the annotations are sound.
  /// </summary>
  /// <returns>True if the annotations pass, false otherwise.</returns>
  public Option<State<T, TS>> CheckAnnotations()
  {
    return CheckBaseCase().OrElse(CheckInductive).OrElse(CheckAssertions);
  }

  public Option<State<T, TS>> CheckBaseCase(string node)
  {
    var route = Symbolic<T>();

    // if the route is the initial value, then the annotation holds (i.e., the annotation contains the route at time 0).
    var check = Implies(route == InitialValues[node],
      Annotations[node](route, new BigInteger(0)));

    // negate and try to prove unsatisfiable.
    var model = And(GetAssumptions(), Not(check)).Solve();

    if (!model.IsSatisfiable()) return Option.None<State<T, TS>>();
    var state = new State<T, TS>(model, node, route, Option.None<Zen<BigInteger>>(), Symbolics, SmtCheck.Base);
    return Option.Some(state);
  }

  /// <summary>
  /// Ensure that all the base check pass for all nodes.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckBaseCase()
  {
    return Topology.Nodes.AsParallel().Aggregate(Option.None<State<T, TS>>(),
      (current, node) => current.OrElse(() => CheckBaseCase(node)));
  }

  /// <summary>
  /// Ensure that the inductive invariants imply the assertions.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckAssertions()
  {
    return Topology.Nodes.AsParallel().Aggregate(Option.None<State<T, TS>>(),
      (current, node) => current.OrElse(() => CheckAssertions(node)));
  }

  public Option<State<T, TS>> CheckAssertions(string node)
  {
    var route = Symbolic<T>();
    var time = Symbolic<BigInteger>();

    // ensure the inductive invariant implies the assertions we want to prove.
    var check = Implies(Annotations[node](route, time), ModularProperties[node](route, time));

    // negate and try to prove unsatisfiable.
    var model = And(GetAssumptions(), Not(check)).Solve();

    if (!model.IsSatisfiable()) return Option.None<State<T, TS>>();
    var state = new State<T, TS>(model, node, route, Option.Some(time), Symbolics, SmtCheck.Safety);
    return Option.Some(state);
  }

  /// <summary>
  /// Ensure that the inductive checks all pass.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckInductive()
  {
    // create symbolic values for each node.
    var routes = Topology.MapNodes(_ => Symbolic<T>());

    // create a symbolic time variable.
    var time = Symbolic<BigInteger>();

    // check the inductive invariant for each node.
    // Parallel.ForEach(Topology.Nodes, node => CheckInductive(node, routes, time))
    return Topology.Nodes.AsParallel().Select(
        node => CheckInductive(node, routes, time))
      .Aggregate(Option.None<State<T, TS>>(), (current, s) => current.OrElse(() => s));
  }

  /// <summary>
  /// Test the inductive check for the given node.
  /// </summary>
  /// <param name="node">The node to test.</param>
  /// <param name="routes">A dictionary mapping nodes to symbolic routes.</param>
  /// <param name="time">The symbolic time to test.</param>
  /// <returns>Some State if a counterexample is returned where the inductive check does not hold,
  /// and otherwise None.</returns>
  public Option<State<T, TS>> CheckInductive(string node, IReadOnlyDictionary<string, Zen<T>> routes,
    Zen<BigInteger> time)
  {
    // get the new route as the merge of all neighbors
    var newNodeRoute = UpdateNodeRoute(node, routes);

    // collect all of the symbolics from neighbors.
    var assume = new List<Zen<bool>> {time > new BigInteger(0)};
    assume.AddRange(Topology[node].Select(neighbor =>
      Annotations[neighbor](routes[neighbor], time - new BigInteger(1))));

    // now we need to ensure the new route after merging implies the annotation for this node.
    var check = Implies(And(assume.ToArray()), Annotations[node](newNodeRoute, time));

    // negate and try to prove unsatisfiable.
    var model = And(GetAssumptions(), Not(check)).Solve();

    if (!model.IsSatisfiable()) return Option.None<State<T, TS>>();
    var neighborRoutes = routes.Where(pair => Topology[node].Contains(pair.Key));
    var state = new State<T, TS>(model, node, newNodeRoute, neighborRoutes, time, Symbolics);
    return Option.Some(state);
  }

  /// <summary>
  /// Check the network using a stable routes encoding.
  /// </summary>
  /// <returns>Some state if verification fails with a counterexample, and None otherwise.</returns>
  public Option<State<T, TS>> CheckMonolithic()
  {
    // create symbolic values for each node.
    var routes = Topology.MapNodes(_ => Symbolic<T>());

    // add the assertions
    var assertions = Topology.Nodes.Select(node => MonolithicProperties[node](routes[node]));

    // add constraints for each node, that its route is the merge of all the neighbors and init
    var constraints = Topology.Nodes.Select(node =>
      routes[node] == UpdateNodeRoute(node, routes));

    var check = And(GetAssumptions(), And(constraints.ToArray()), Not(And(assertions.ToArray())));

    // negate and try to prove unsatisfiable.
    var model = check.Solve();

    if (model.IsSatisfiable())
    {
      return Option.Some(new State<T, TS>(model, routes, Symbolics));
    }

    Console.WriteLine("    The monolithic checks passed!");
    return Option.None<State<T, TS>>();
  }

  private Zen<T> UpdateNodeRoute(string node, IReadOnlyDictionary<string, Zen<T>> routes)
  {
    return Topology[node].Aggregate(InitialValues[node],
      (current, predecessor) =>
        MergeFunction(current, TransferFunction[(predecessor, node)](routes[predecessor])));
  }

  private Zen<bool> GetAssumptions()
  {
    var assumptions = Symbolics.Where(p => p.HasConstraint()).Select(p => p.Encode()).ToArray();
    // And() with an empty array throws an error, so we check the length first
    return assumptions.Length == 0 ? True() : And(assumptions);
  }
}
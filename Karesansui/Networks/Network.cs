using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Karesansui.Datatypes;
using ZenLib;
using static ZenLib.Zen;

namespace Karesansui.Networks;

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
  public Dictionary<string, Zen<T>> InitialValues { get; protected init; }

  /// <summary>
  /// The merge function for routes.
  /// </summary>
  public Func<Zen<T>, Zen<T>, Zen<T>> MergeFunction { get; }

  /// <summary>
  /// The modular safety properties that we want to check (includes time).
  /// </summary>
  private readonly Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> _modularProperties;

  /// <summary>
  /// The monolithic safety properties that we want to check (assumes stable states).
  /// </summary>
  private readonly Dictionary<string, Func<Zen<T>, Zen<bool>>> _monolithicProperties;

  /// <summary>
  /// Any additional symbolics on the network's components.
  /// </summary>
  protected SymbolicValue<TS>[] symbolics;

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
  protected Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> Annotations;

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
    this.symbolics = symbolics;
    Annotations = annotations;
    _modularProperties = modularProperties;
    _monolithicProperties = monolithicProperties;
  }

  /// <summary>
  /// Check that the annotations are sound.
  /// </summary>
  /// <returns>True if the annotations pass, false otherwise.</returns>
  public Option<State<T, TS>> CheckAnnotations()
  {
    return CheckBaseCase().OrElse(CheckAssertions).OrElse(CheckInductive);
  }

  public Option<State<T, TS>> CheckBaseCase(string node)
  {
    var route = Symbolic<T>();

    // if the route is the initial value, then the annotation holds (i.e., the annotation contains the route at time 0).
    var check = Implies(route == InitialValues[node],
      Annotations[node](route, new BigInteger(0)));

    // negate and try to prove unsat.
    var model = And(GetAssumptions(), Not(check)).Solve();

    if (!model.IsSatisfiable()) return Option.None<State<T, TS>>();
    var state = new State<T, TS>(model, node, route, Option.None<Zen<BigInteger>>(), symbolics, "base");
    return Option.Some(state);
  }

  /// <summary>
  /// Ensure that all the base check pass for all nodes.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckBaseCase()
  {
    return Topology.Nodes.Aggregate(Option.None<State<T, TS>>(),
      (current, node) => current.OrElse(() => CheckBaseCase(node)));
  }

  /// <summary>
  /// Ensure that the inductive invariants imply the assertions.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckAssertions()
  {
    return Topology.Nodes.Aggregate(Option.None<State<T, TS>>(),
      (current, node) => current.OrElse(() => CheckAssertions(node)));
  }

  public Option<State<T, TS>> CheckAssertions(string node)
  {
    var route = Symbolic<T>();
    var time = Symbolic<BigInteger>();

    // ensure the inductive invariant implies the assertions we want to prove.
    var check = Implies(Annotations[node](route, time), _modularProperties[node](route, time));

    // negate and try to prove unsat.
    var model = And(GetAssumptions(), Not(check)).Solve();

    if (!model.IsSatisfiable()) return Option.None<State<T, TS>>();
    var state = new State<T, TS>(model, node, route, Option.Some(time), symbolics, "assertion");
    return Option.Some(state);
  }

  /// <summary>
  /// Ensure that the inductive checks all pass.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckInductive()
  {
    // create symbolic values for each node.
    var routes = new Dictionary<string, Zen<T>>();
    foreach (var node in Topology.Nodes) routes[node] = Symbolic<T>();

    // create a symbolic time variable.
    var time = Symbolic<BigInteger>();

    // check the inductiveness of the invariant for each node.
    return Topology.Nodes.Aggregate(Option.None<State<T, TS>>(),
      (current, node) => current.OrElse(() => CheckInductive(node, routes, time)));
  }

  /// <summary>
  /// Test the inductive check for the given node.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="routes"></param>
  /// <param name="time"></param>
  /// <returns></returns>
  public Option<State<T, TS>> CheckInductive(string node, Dictionary<string, Zen<T>> routes, Zen<BigInteger> time)
  {
    // get the new route as the merge of all neighbors
    var newNodeRoute = UpdateNodeRoute(node, routes);

    // collect all of the symbolics from neighbors.
    var assume = new List<Zen<bool>> {time > new BigInteger(0)};
    assume.AddRange(Topology[node].Select(neighbor =>
      Annotations[neighbor](routes[neighbor], time - new BigInteger(1))));

    // now we need to ensure the new route after merging implies the annotation for this node.
    var check = Implies(And(assume.ToArray()), Annotations[node](newNodeRoute, time));

    // negate and try to prove unsat.
    var model = And(GetAssumptions(), Not(check)).Solve();

    if (!model.IsSatisfiable()) return Option.None<State<T, TS>>();
    var neighborRoutes = routes.Where(pair => Topology[node].Contains(pair.Key));
    var state = new State<T, TS>(model, node, neighborRoutes, Option.Some(time), symbolics, "inductive");
    return Option.Some(state);
  }

  /// <summary>
  /// Check the network using a stable routes encoding.
  /// </summary>
  /// <returns>Some state if verification fails with a counterexample, and None otherwise.</returns>
  public Option<State<T, TS>> CheckMonolithic()
  {
    // create symbolic values for each node.
    var routes = new Dictionary<string, Zen<T>>();
    foreach (var node in Topology.Nodes) routes[node] = Symbolic<T>();

    // add the assertions
    var assertions = Topology.Nodes.Select(node => _monolithicProperties[node](routes[node]));

    // add constraints for each node, that its route is the merge of all the neighbors and init
    var constraints = Topology.Nodes.Select(node =>
      routes[node] == UpdateNodeRoute(node, routes));

    var check = And(GetAssumptions(), And(constraints.ToArray()), Not(And(assertions.ToArray())));

    // negate and try to prove unsat.
    var model = check.Solve();

    if (model.IsSatisfiable())
    {
      return Option.Some(new State<T, TS>(model, routes, Option.None<Zen<BigInteger>>(), symbolics, "monolithic"));
    }

    Console.WriteLine("    The monolithic checks passed!");
    return Option.None<State<T, TS>>();
  }

  private Zen<T> UpdateNodeRoute(string node, IReadOnlyDictionary<string, Zen<T>> neighborRoutes)
  {
    return Topology[node].Aggregate(InitialValues[node],
      (current, neighbor) =>
        MergeFunction(current, TransferFunction[(neighbor, node)](neighborRoutes[neighbor])));
  }

  private Zen<bool> GetAssumptions()
  {
    return And(symbolics.Where(p => p.HasConstraint()).Select(p => p.Encode()).ToArray());
  }
}

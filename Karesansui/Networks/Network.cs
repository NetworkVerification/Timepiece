using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

/// <summary>
///     Represents an NV network.
/// </summary>
/// <typeparam name="T">The type of the routes.</typeparam>
/// <typeparam name="TS">The type of symbolic values associated with the network.</typeparam>
public class Network<T, TS>
{
  /// <summary>
  ///     The initial values for each node.
  /// </summary>
  private readonly Dictionary<string, Zen<T>> initialValues;

  /// <summary>
  ///     The merge function for routes.
  /// </summary>
  private readonly Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction;

  /// <summary>
  ///     The modular safety properties that we want to check (includes time).
  /// </summary>
  protected readonly Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> modularProperties;

  /// <summary>
  ///     The monolithic safety properties that we want to check (assumes stable states).
  /// </summary>
  protected readonly Dictionary<string, Func<Zen<T>, Zen<bool>>> monolithicProperties;

  /// <summary>
  ///     Any additional symbolics on the network's components.
  /// </summary>
  private readonly SymbolicValue<TS>[] symbolics;

  /// <summary>
  ///     The topology of the network.
  /// </summary>
  private readonly Topology topology;

  /// <summary>
  ///     The transfer function for each edge.
  /// </summary>
  private readonly Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction;

  /// <summary>
  ///     The invariant/annotation function for each node. Takes a route and a time and returns a boolean.
  /// </summary>
  protected Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations;

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
    this.topology = topology;
    this.transferFunction = transferFunction;
    this.mergeFunction = mergeFunction;
    this.initialValues = initialValues;
    this.symbolics = symbolics;
    this.annotations = annotations;
    this.modularProperties = modularProperties;
    this.monolithicProperties = monolithicProperties;
  }

  /// <summary>
  ///     Check that the annotations are sound.
  /// </summary>
  /// <returns>True if the annotations pass, false otherwise.</returns>
  public bool CheckAnnotations()
  {
    return !CheckBaseCase().OrElse(CheckAssertions).OrElse(CheckInductive).HasValue;
  }

  /// <summary>
  ///     Ensure that all the base check pass.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckBaseCase()
  {
    foreach (var node in topology.nodes)
    {
      var route = Symbolic<T>();

      // if the route is the initial value, then the annotation holds (i.e., the annotation contains the route at time 0).
      var check = Implies(route == initialValues[node],
        annotations[node](route, new BigInteger(0)));

      // negate and try to prove unsat.
      var model = And(GetAssumptions(), Not(check)).Solve();

      if (!model.IsSatisfiable()) continue;
      Console.ForegroundColor = ConsoleColor.Red;
      var state = new State<T, TS>(model, node, route, Option.None<Zen<BigInteger>>(), symbolics);
      // state.ReportCheckFailure("base");
      return Option.Some(state);
    }

    Console.WriteLine("    All the base checks passed!");
    return Option.None<State<T, TS>>();
  }

  /// <summary>
  ///     Ensure that the inductive invariants imply the assertions.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckAssertions()
  {
    foreach (var node in topology.nodes)
    {
      var route = Symbolic<T>();
      var time = Symbolic<BigInteger>();

      // ensure the inductive invariant implies the assertions we want to prove.
      var check = Implies(annotations[node](route, time), modularProperties[node](route, time));

      // negate and try to prove unsat.
      var model = And(GetAssumptions(), Not(check)).Solve();

      if (!model.IsSatisfiable()) continue;
      var state = new State<T, TS>(model, node, route, Option.Some(time), symbolics);
      state.ReportCheckFailure("assertion");
      return Option.Some(state);
    }

    Console.WriteLine("    All the assertions checks passed!");
    return Option.None<State<T, TS>>();
  }

  /// <summary>
  ///     Ensure that the inductive checks all pass.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<T, TS>> CheckInductive()
  {
    // create symbolic values for each node.
    var routes = new Dictionary<string, Zen<T>>();
    foreach (var node in topology.nodes) routes[node] = Symbolic<T>();

    // create a symbolic time variable.
    var time = Symbolic<BigInteger>();

    // check the inductiveness of the invariant for each node.
    foreach (var node in topology.nodes)
    {
      // get the new route as the merge of all neighbors
      var newNodeRoute = UpdateNodeRoute(node, routes);

      // collect all of the symbolics from neighbors.
      var assume = new List<Zen<bool>> {time > new BigInteger(0)};
      assume.AddRange(topology[node].Select(neighbor =>
        annotations[neighbor](routes[neighbor], time - new BigInteger(1))));

      // now we need to ensure the new route after merging implies the annotation for this node.
      var check = Implies(And(assume.ToArray()), annotations[node](newNodeRoute, time));

      // negate and try to prove unsat.
      var model = And(GetAssumptions(), Not(check)).Solve();

      if (!model.IsSatisfiable()) continue;
      var neighborRoutes = routes.Where(pair => topology[node].Contains(pair.Key));
      var state = new State<T, TS>(model, node, neighborRoutes, Option.Some(time), symbolics);
      state.ReportCheckFailure("inductive");
      return Option.Some(state);
    }

    Console.WriteLine("    All the inductive checks passed!");
    return Option.None<State<T, TS>>();
  }

  /// <summary>
  ///     Check the network using a stable routes encoding.
  /// </summary>
  /// <returns>True if the network verifies, false otherwise.</returns>
  public bool CheckMonolithic()
  {
    // create symbolic values for each node.
    var routes = new Dictionary<string, Zen<T>>();
    foreach (var node in topology.nodes) routes[node] = Symbolic<T>();

    // add the assertions
    var assertions = topology.nodes.Select(node => monolithicProperties[node](routes[node]));

    // add constraints for each node, that its route is the merge of all the neighbors and init
    var constraints = topology.nodes.Select(node =>
      routes[node] == UpdateNodeRoute(node, routes));

    var check = And(GetAssumptions(), And(constraints.ToArray()), Not(And(assertions.ToArray())));

    // negate and try to prove unsat.
    var model = check.Solve();

    if (model.IsSatisfiable())
    {
      var state = new State<T, TS>(model, routes, Option.None<Zen<BigInteger>>(), symbolics);
      state.ReportCheckFailure("monolithic");
      return false;
    }

    Console.WriteLine("    The monolithic checks passed!");
    return true;
  }

  private Zen<T> UpdateNodeRoute(string node, IReadOnlyDictionary<string, Zen<T>> neighborRoutes)
  {
    return topology[node].Aggregate(initialValues[node],
      (current, neighbor) =>
        mergeFunction(current, transferFunction[(neighbor, node)](neighborRoutes[neighbor])));
  }

  private Zen<bool> GetAssumptions()
  {
    return And(symbolics.Where(p => p.HasConstraint()).Select(p => p.Encode()).ToArray());
  }
}

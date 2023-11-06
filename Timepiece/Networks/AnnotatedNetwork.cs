#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.DataTypes;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

/// <summary>
///   Represents an annotated network.
/// </summary>
/// <typeparam name="RouteType">The type of the routes.</typeparam>
/// <typeparam name="NodeType">The type of nodes.</typeparam>
public class AnnotatedNetwork<RouteType, NodeType> : Network<RouteType, NodeType> where NodeType : notnull
{
  /// <summary>
  ///   Construct a new <c>AnnotatedNetwork{T,TV,TS}</c>.
  /// </summary>
  /// <param name="digraph">The network topology.</param>
  /// <param name="transferFunction">A Dictionary from edges to transfer functions.</param>
  /// <param name="mergeFunction">The merge function.</param>
  /// <param name="initialValues">A Dictionary from nodes to initial routes.</param>
  /// <param name="annotations">A Dictionary from nodes to temporal predicates.</param>
  /// <param name="modularProperties">A Dictionary from nodes to temporal predicates.</param>
  /// <param name="monolithicProperties">A Dictionary from nodes to predicates.</param>
  /// <param name="symbolics">An array of symbolic values.</param>
  public AnnotatedNetwork(
    Digraph<NodeType> digraph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunction,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<NodeType, Zen<RouteType>> initialValues,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties,
    ISymbolic[] symbolics) : base(digraph, transferFunction, mergeFunction, initialValues, symbolics)
  {
    Annotations = annotations;
    ModularProperties = modularProperties;
    MonolithicProperties = monolithicProperties;
  }

  /// <summary>
  ///   Construct a new <c>AnnotatedNetwork{T,TS}</c> using an alternate properties definition.
  ///   For modular checking, for each node we check a property
  ///   <c>Globally(safetyProperties[n])</c> and <c>Finally(convergeTime, stableProperties[n])</c>.
  ///   For monolithic checking, for each node we check a property
  ///   <c>safetyProperties[n]</c> and <c>stableProperties[n]</c>.
  /// </summary>
  /// <param name="digraph">The network topology.</param>
  /// <param name="transferFunction"></param>
  /// <param name="mergeFunction"></param>
  /// <param name="initialValues"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties">Properties which are true at convergence.</param>
  /// <param name="safetyProperties">Properties which are always true (in the modular network).</param>
  /// <param name="convergeTime">The convergence time of the network.</param>
  /// <param name="symbolics"></param>
  public AnnotatedNetwork(Digraph<NodeType> digraph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunction,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<NodeType, Zen<RouteType>> initialValues,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime,
    ISymbolic[] symbolics) : this(digraph,
    transferFunction, mergeFunction, initialValues, annotations,
    // modular properties: Finally(stable) + Globally(safety)
    digraph.MapNodes(n =>
      Lang.Intersect(Lang.Finally(convergeTime, stableProperties[n]), Lang.Globally(safetyProperties[n]))),
    // monolithic properties: stable + safety
    digraph.MapNodes(n => Lang.Intersect(stableProperties[n], safetyProperties[n])),
    symbolics)
  {
  }

  /// <summary>
  ///   Construct a new <c>AnnotatedNetwork{T,TS}</c> from another.
  /// </summary>
  /// <param name="net"></param>
  /// <param name="annotations"></param>
  /// <param name="modularProperties"></param>
  /// <param name="monolithicProperties"></param>
  public AnnotatedNetwork(Network<RouteType, NodeType> net,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties) : this(net.Digraph,
    net.TransferFunction,
    net.MergeFunction,
    net.InitialValues, annotations, modularProperties, monolithicProperties, net.Symbolics)
  {
  }

  /// <summary>
  ///   Construct a new <c>AnnotatedNetwork{T,TS}</c> from another, using an alternate properties definition.
  /// </summary>
  /// <param name="net"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="convergeTime"></param>
  public AnnotatedNetwork(Network<RouteType, NodeType> net,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> safetyProperties,
    BigInteger convergeTime) : this(net.Digraph, net.TransferFunction, net.MergeFunction,
    net.InitialValues, annotations, stableProperties, safetyProperties, convergeTime, net.Symbolics)
  {
  }

  /// <summary>
  ///   The modular safety properties that we want to check (includes time).
  /// </summary>
  public Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> ModularProperties { get; set; }

  /// <summary>
  ///   The monolithic safety properties that we want to check (assumes stable states).
  /// </summary>
  public Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> MonolithicProperties { get; set; }

  /// <summary>
  ///   The invariant/annotation function for each node. Takes a route and a time and returns a boolean.
  /// </summary>
  public Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> Annotations { get; set; }

  /// <summary>
  ///   Setting to control whether or not to print the constructed formulae to the user.
  /// </summary>
  public bool PrintFormulas { get; set; }

  /// <summary>
  ///   The maximum number of logical time steps of delay to consider.
  /// </summary>
  public Zen<BigInteger>? MaxDelay { get; set; }

  /// <summary>
  ///   Verify that the annotations are sound, calling the given function f on each node's check.
  /// </summary>
  /// <param name="collector"></param>
  /// <param name="f"></param>
  /// <returns></returns>
  public Dictionary<NodeType, Option<State<RouteType, NodeType>>> CheckAnnotationsWith<TAcc>(
    TAcc collector,
    Func<NodeType, TAcc, Func<Option<State<RouteType, NodeType>>>,
      Option<State<RouteType, NodeType>>> f)
  {
    var routes = Digraph.MapNodes(node => Symbolic<RouteType>($"{node}-route"));
    var time = Symbolic<BigInteger>("time");
    var s = Digraph.Nodes
      // call f for each node
      .AsParallel()
      .Select(node => (node, f(node, collector, () => CheckAnnotations(node, routes, time))))
      .ToDictionary(x => x.Item1, x => x.Item2);
    return s;
  }

  public Option<State<RouteType, NodeType>> CheckAnnotations(NodeType node)
  {
    return CheckAnnotations(node, Digraph[node].ToDictionary(n => n, n => Symbolic<RouteType>($"{n}-route")),
      Symbolic<BigInteger>("time"));
  }

  private Option<State<RouteType, NodeType>> CheckAnnotations(NodeType node,
    IReadOnlyDictionary<NodeType, Zen<RouteType>> routes,
    Zen<BigInteger> time)
  {
    return CheckInitial(node).OrElse(() => CheckInductive(node, routes, time)).OrElse(() => CheckSafety(node));
  }

  /// <summary>
  ///   Verify that the annotations are sound.
  /// </summary>
  /// <returns>True if the annotations pass, false otherwise.</returns>
  public Option<State<RouteType, NodeType>> CheckAnnotations()
  {
    return CheckInitial().OrElse(CheckInductive).OrElse(CheckSafety);
  }

  public Option<State<RouteType, NodeType>> CheckAnnotationsDelayed()
  {
    return CheckInitial().OrElse(CheckInductiveDelayed).OrElse(CheckSafety);
  }

  public Option<State<RouteType, NodeType>> CheckInitial(NodeType node)
  {
    var route = Symbolic<RouteType>($"{node}-route");

    // if the route is the initial value, then the annotation holds (i.e., the annotation contains the route at time 0).
    var check = Implies(route == InitialValues[node],
      Annotations[node](route, new BigInteger(0)));

    // negate and try to prove unsatisfiable.
    var query = And(GetSymbolicConstraints(), Not(check));
    if (PrintFormulas)
    {
      Console.Write($"Initial check at {node}: ");
      Console.WriteLine(query.Format());
    }

    var model = query.Solve();

    if (!model.IsSatisfiable()) return Option.None<State<RouteType, NodeType>>();
    var state = new State<RouteType, NodeType>(model, node, route, Option.None<Zen<BigInteger>>(),
      Symbolics, SmtCheck.Initial);
    return Option.Some(state);
  }

  /// <summary>
  ///   Ensure that all the initial check pass for all nodes.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<RouteType, NodeType>> CheckInitial()
  {
    return Digraph.Nodes.AsParallel().Aggregate(Option.None<State<RouteType, NodeType>>(),
      (current, node) => current.OrElse(() => CheckInitial(node)));
  }

  /// <summary>
  ///   Ensure that the inductive invariants imply the assertions.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<RouteType, NodeType>> CheckSafety()
  {
    return Digraph.Nodes.AsParallel().Aggregate(Option.None<State<RouteType, NodeType>>(),
      (current, node) => current.OrElse(() => CheckSafety(node)));
  }

  public Option<State<RouteType, NodeType>> CheckSafety(NodeType node)
  {
    var route = Symbolic<RouteType>($"{node}-route");
    var time = Symbolic<BigInteger>("time");

    // ensure the inductive invariant implies the assertions we want to prove.
    var check = Implies(Annotations[node](route, time), ModularProperties[node](route, time));

    // negate and try to prove unsatisfiable.
    var query = And(GetSymbolicConstraints(), Not(check));
    if (PrintFormulas)
    {
      Console.Write($"Safety check at {node}: ");
      Console.WriteLine(query.Format());
    }

    var model = query.Solve();

    if (!model.IsSatisfiable()) return Option.None<State<RouteType, NodeType>>();
    var state = new State<RouteType, NodeType>(model, node, route, Option.Some(time), Symbolics,
      SmtCheck.Safety);
    return Option.Some(state);
  }

  /// <summary>
  ///   Ensure that the inductive checks all pass.
  /// </summary>
  /// <returns>None if the annotations pass, a counterexample State otherwise.</returns>
  public Option<State<RouteType, NodeType>> CheckInductive()
  {
    // create symbolic values for each node.
    var routes = Digraph.MapNodes(node => Symbolic<RouteType>($"{node}-route"));

    // create a symbolic time variable.
    var time = Symbolic<BigInteger>("time");

    // check the inductive invariant for each node.
    // Parallel.ForEach(Topology.Nodes, node => CheckInductive(node, routes, time))
    return Digraph.Nodes.AsParallel().Select(
        node => CheckInductive(node, routes, time))
      .Aggregate(Option.None<State<RouteType, NodeType>>(), (current, s) => current.OrElse(() => s));
  }

  /// <summary>
  /// Ensure that a particular node's inductive check passes.
  /// </summary>
  /// <param name="node"></param>
  /// <returns></returns>
  /// <remarks>
  ///   Each call to this function initializes its own mapping of nodes to routes and a symbolic time.
  /// </remarks>
  public Option<State<RouteType, NodeType>> CheckInductive(NodeType node)
  {
    // create symbolic values for each node neighbor.
    var routes = Digraph[node].ToDictionary(n => n, n => Symbolic<RouteType>($"{n}-route"));

    // create a symbolic time variable.
    var time = Symbolic<BigInteger>("time");
    return CheckInductive(node, routes, time);
  }

  /// <summary>
  ///   Test the inductive check for the given node.
  /// </summary>
  /// <param name="node">The node to test.</param>
  /// <param name="routes">A dictionary mapping nodes to symbolic routes.</param>
  /// <param name="time">The symbolic time to test.</param>
  /// <returns>
  ///   Some State if a counterexample is returned where the inductive check does not hold,
  ///   and otherwise None.
  /// </returns>
  public Option<State<RouteType, NodeType>> CheckInductive(NodeType node,
    IReadOnlyDictionary<NodeType, Zen<RouteType>> routes,
    Zen<BigInteger> time)
  {
    // get the new route as the merge of all neighbors
    var newNodeRoute = UpdateNodeRoute(node, routes);

    // collect all of the symbolics from neighbors.
    var assume = new List<Zen<bool>> {time > new BigInteger(0)};
    assume.AddRange(Digraph[node].Select(neighbor =>
      Annotations[neighbor](routes[neighbor], time - new BigInteger(1))));

    // now we need to ensure the new route after merging implies the annotation for this node.
    var check = Implies(And(assume.ToArray()), Annotations[node](newNodeRoute, time));

    // negate and try to prove unsatisfiable.
    var query = And(GetSymbolicConstraints(), Not(check));
    if (PrintFormulas)
    {
      Console.Write($"Inductive check at {node}: ");
      Console.WriteLine(query.Format());
    }

    var model = query.Solve();

    if (!model.IsSatisfiable()) return Option.None<State<RouteType, NodeType>>();
    var neighborRoutes = routes.Where(pair => Digraph[node].Contains(pair.Key));
    var state = new State<RouteType, NodeType>(model, node, newNodeRoute, neighborRoutes, time,
      Symbolics);
    return Option.Some(state);
  }

  public Option<State<RouteType, NodeType>> CheckInductiveDelayed()
  {
    // create symbolic values for each node.
    var routes = Digraph.MapNodes(node => Symbolic<RouteType>($"{node}-route"));

    // create symbolic time variables for each node.
    var times = Digraph.MapNodes(node => Symbolic<BigInteger>($"{node}-time"));

    // check the inductive invariant for each node.
    // Parallel.ForEach(Topology.Nodes, node => CheckInductive(node, routes, time))
    return Digraph.Nodes.AsParallel().Select(
        node => CheckInductiveDelayed(node, routes, times))
      .Aggregate(Option.None<State<RouteType, NodeType>>(), (current, s) => current.OrElse(() => s));
  }

  public Option<State<RouteType, NodeType>> CheckInductiveDelayed(NodeType node,
    IReadOnlyDictionary<NodeType, Zen<RouteType>> routes, IReadOnlyDictionary<NodeType, Zen<BigInteger>> times)
  {
    var newNodeRoute = UpdateNodeRoute(node, routes);

    var assume = new List<Zen<bool>> {times[node] > BigInteger.Zero};
    // constrain all neighbor times to be earlier than the node's time
    assume.AddRange(Digraph[node]
      .Select(neighbor => And(
        // neighbor times are non-negative
        BigInteger.Zero <= times[neighbor],
        // neighbor times are earlier than the node's
        times[neighbor] < times[node],
        // if a max delay k is given, the neighbor times are at most k units smaller than the node's time
        MaxDelay is not null ? times[node] <= times[neighbor] + MaxDelay : True())));
    // constrain all neighbor routes to satisfy their annotations at those earlier times
    assume.AddRange(Digraph[node].Select(neighbor =>
      Annotations[neighbor](routes[neighbor], times[neighbor])));
    var check = Implies(And(assume.ToArray()), Annotations[node](newNodeRoute, times[node]));

    var query = And(GetSymbolicConstraints(), Not(check));
    if (PrintFormulas)
    {
      Console.Write($"Inductive (delayed) check at {node}: ");
      Console.WriteLine(query.Format());
    }

    var model = query.Solve();

    if (!model.IsSatisfiable()) return Option.None<State<RouteType, NodeType>>();
    var neighborRoutes = routes.Where(pair => Digraph[node].Contains(pair.Key));
    var neighborTimes = times.Where(pair => Digraph[node].Contains(pair.Key));
    var state = new State<RouteType, NodeType>(model, node, newNodeRoute, neighborRoutes, times[node], neighborTimes,
      Symbolics);
    return Option.Some(state);
  }

  /// <summary>
  ///   Verify the network using a stable routes encoding.
  /// </summary>
  /// <returns>Some state if verification fails with a counterexample, and None otherwise.</returns>
  public Option<State<RouteType, NodeType>> CheckMonolithic()
  {
    // create symbolic values for each node.
    var routes = Digraph.MapNodes(node => Symbolic<RouteType>($"{node}-route"));

    // add the assertions
    var assertions = Digraph.Nodes.Select(node => MonolithicProperties[node](routes[node]));

    // add constraints for each node, that its route is the merge of all the neighbors and init
    var constraints = Digraph.Nodes.Select(node =>
      routes[node] == UpdateNodeRoute(node, routes));

    var query = And(GetSymbolicConstraints(), And(constraints.ToArray()), Not(And(assertions.ToArray())));
    if (PrintFormulas)
    {
      Console.WriteLine("Monolithic check:");
      Console.WriteLine(query.Format());
    }

    // negate and try to prove unsatisfiable.
    var model = query.Solve();

    if (model.IsSatisfiable())
      return Option.Some(new State<RouteType, NodeType>(model, routes, Symbolics));

    Console.WriteLine("    The monolithic checks passed!");
    return Option.None<State<RouteType, NodeType>>();
  }
}

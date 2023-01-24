using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Timepiece;
using Timepiece.Networks;
using ZenLib;

namespace MisterWolf;

public class Infer<T>
{
  public Infer(Topology topology,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<string, Zen<T>> initialValues,
    Dictionary<string, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    Dictionary<string, Func<Zen<T>, Zen<bool>>> afterInvariants
  )
  {
    Topology = topology;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitialValues = initialValues;
    BeforeInvariants = beforeInvariants;
    AfterInvariants = afterInvariants;
  }

  public Topology Topology { get; }
  public Dictionary<(string, string), Func<Zen<T>, Zen<T>>> TransferFunction { get; }
  public Func<Zen<T>, Zen<T>, Zen<T>> MergeFunction { get; }
  public Dictionary<string, Zen<T>> InitialValues { get; }
  public Dictionary<string, Func<Zen<T>, Zen<bool>>> BeforeInvariants { get; }
  public Dictionary<string, Func<Zen<T>, Zen<bool>>> AfterInvariants { get; }

  /// <summary>
  /// Check that the given node's invariant is implied by the invariants of the given ancestors.
  /// An ancestor is a predecessor of the given node which has already converged to its after invariant.
  /// Return true if the invariant is implied by the invariants, and false otherwise.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="invariant">A predicate to check on the node.</param>
  /// <param name="ancestors">A subset of the node's predecessors.</param>
  /// <returns>True if the invariant is always satisfied by the ancestors, and false otherwise.</returns>
  private bool CheckAncestors(string node, Func<Zen<T>, Zen<bool>> invariant, IEnumerable<string> ancestors)
  {
    var routes = new Dictionary<string, Zen<T>>();
    foreach (var predecessor in Topology[node])
    {
      routes[predecessor] = Zen.Symbolic<T>();
    }

    var newNodeRoute = UpdateNodeRoute(node, routes);

    // check predecessor invariants according to whether or not the predecessor was given in ancestors
    // we check the after invariant of an included predecessor, and otherwise the before invariant
    var assume = Topology[node]
      .Select(pred =>
        ancestors.Contains(pred) ? AfterInvariants[pred](routes[pred]) : BeforeInvariants[pred](routes[pred]));
    var check = Zen.Implies(Zen.And(assume.ToArray()), invariant(newNodeRoute));

    var query = Zen.Not(check);
    var model = query.Solve();

    if (!model.IsSatisfiable()) return true;
    var ancestorString = new StringBuilder();
    foreach (var ancestor in ancestors)
    {
      ancestorString.Append($", {ancestor}");
    }

    Console.WriteLine($"Ancestors [{ancestorString}] do NOT imply node {node}'s invariant.");
    return false;
  }

  /// <summary>
  /// Return a route corresponding to the application of one step of the network semantics:
  /// starting from the initial route at a node, merge in each transferred route from the node's neighbor.
  /// </summary>
  /// <param name="node">The focal node.</param>
  /// <param name="routes">The routes of all nodes in the network.</param>
  /// <returns>A route.</returns>
  private Zen<T> UpdateNodeRoute(string node, IReadOnlyDictionary<string, Zen<T>> routes)
  {
    return Topology[node].Aggregate(InitialValues[node],
      (current, predecessor) =>
        MergeFunction(current, TransferFunction[(predecessor, node)](routes[predecessor])));
  }

  /// <summary>
  /// Return the power set of an enumerable of elements.
  /// </summary>
  /// <param name="set"></param>
  /// <returns></returns>
  private static IEnumerable<IEnumerable<TElement>> PowerSet<TElement>(IEnumerable<TElement> set)
  {
    var sets = new List<IEnumerable<TElement>> {Enumerable.Empty<TElement>()};
    return set.Aggregate(sets,
      // given the current set of sets, add new sets which extend them all by an element
      (currentSets, element) =>
        currentSets.Concat(currentSets.Select(s => s.Concat(new[] {element}))).ToList());
  }

  public Dictionary<string, BigInteger> InferTimes()
  {
    // for each node, for each subset of its predecessors, run CheckAncestors in parallel
    // construct a dictionary of the results of which ancestors fail to imply the two invariants
    var afterChecks = new ConcurrentDictionary<string, string[]>();
    var beforeChecks = new ConcurrentDictionary<string, string[]>();
    var nodeAndAncestors = Topology.Nodes
      .SelectMany(n => PowerSet(Topology[n]), (n, ancestors) => (n, ancestors));
    nodeAndAncestors.AsParallel()
      .ForAll(tuple =>
      {
        var n = tuple.n;
        var anc = tuple.ancestors.ToArray();
        if (!CheckAncestors(n, BeforeInvariants[n], anc))
        {
          beforeChecks[n] = anc;
        }

        if (!CheckAncestors(n, AfterInvariants[n], anc))
        {
          afterChecks[n] = anc;
        }
      });
    // construct a set of bounds to check
    var times = Topology.MapNodes(_ => Zen.Symbolic<BigInteger>());
    var bounds = new List<Zen<bool>>();
    // for each failed check, we add the following bounds:
    // (1) if the before check failed for node n and ancestors anc, add bounds for all ancestors m in anc
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 >= t_n
    foreach (var (node, ancestors) in beforeChecks)
    {
      bounds.Add(Zen.Or(BigInteger.One >= times[node],
        Zen.Not(NextToConverge(Topology[node], BigInteger.Zero, times, ancestors))));
      bounds.AddRange(from ancestor in ancestors
        select Zen.Or(times[ancestor] + BigInteger.One >= times[node],
          Zen.Not(NextToConverge(Topology[node], times[ancestor], times, ancestors))));
    }

    // (2) if the after check failed for node n and ancestors anc, add bounds for all predecessors m of n
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 < t_n,
    //     or n converges before all nodes u_j in anc (t_n - 1 < t_j), or after all nodes u_j not in anc (t_n - 1 >= t_j)
    foreach (var (node, ancestors) in afterChecks)
    {
      bounds.Add(Zen.Or(BigInteger.One < times[node],
        Zen.Not(NextToConverge(Topology[node], BigInteger.Zero, times, ancestors))));
      bounds.AddRange(from ancestor in ancestors
        select Zen.Or(times[ancestor] + BigInteger.One < times[node],
          Zen.Not(NextToConverge(Topology[node], times[ancestor], times, ancestors))));
      // TODO: are we going to need to move this subtraction inside? (to deal with the case where times[node] = 0)
      bounds.Add(Zen.Not(NextToConverge(Topology[node], times[node] - BigInteger.One, times, ancestors)));
    }

    // we now take the conjunction of all the bounds
    // and additionally restrict the times to be non-negative
    var constraints = Zen.And(Zen.And(bounds),
      Zen.And(times.Select(t => t.Value >= BigInteger.Zero)));

    var model = constraints.Solve();
    if (model.IsSatisfiable())
    {
      return new Dictionary<string, BigInteger>(times.Select(pair =>
        new KeyValuePair<string, BigInteger>(pair.Key, model.Get(pair.Value))));
    }

    return new Dictionary<string, BigInteger>();
  }

  /// <summary>
  /// Generate a conjunction of constraints on the given time: for the given predecessors and time,
  /// the constraints require that each of the predecessor's times is at most the given time
  /// if the predecessor is also an ancestor, and otherwise strictly greater than the given time.
  /// </summary>
  /// <param name="predecessors"></param>
  /// <param name="time"></param>
  /// <param name="times"></param>
  /// <param name="ancestors"></param>
  /// <returns></returns>
  private static Zen<bool> NextToConverge(IEnumerable<string> predecessors, Zen<BigInteger> time,
    IReadOnlyDictionary<string, Zen<BigInteger>> times, string[] ancestors)
  {
    return Zen.And(predecessors.Select(j => ancestors.Contains(j) ? time >= times[j] : time < times[j]));
  }

  public Network<T, TS> ToNetwork<TS>(Dictionary<string, BigInteger> times)
  {
    var annotations = Topology.MapNodes(n => Lang.Until(times[n], BeforeInvariants[n], AfterInvariants[n]));
    return new Network<T, TS>(Topology, TransferFunction, MergeFunction, InitialValues, annotations, annotations,
      AfterInvariants, new SymbolicValue<TS>[] { });
  }
}

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Timepiece;
using Timepiece.Networks;
using ZenLib;

namespace MisterWolf;

public class Infer<T, TV, TS> : Network<T, TV, TS> where TV : IEquatable<TV>
{
  private readonly int _processes = Environment.ProcessorCount;
  private ConcurrentDictionary<TV, long> InferInitialTimes { get; set; }
  private ConcurrentDictionary<TV, long> InferInductiveTimes { get; set; }
  private ConcurrentDictionary<(TV, IReadOnlyList<bool>), long> InferBeforeInductiveTimes { get; set; }
  private ConcurrentDictionary<(TV, IReadOnlyList<bool>), long> InferAfterInductiveTimes { get; set; }

  public Infer(Digraph<TV> digraph,
    Dictionary<(TV, TV), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<TV, Zen<T>> initialValues,
    IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> afterInvariants,
    SymbolicValue<TS>[] symbolics) : base(digraph, transferFunction, mergeFunction,
    initialValues, symbolics)
  {
    BeforeInvariants = beforeInvariants;
    AfterInvariants = afterInvariants;
    NumInductiveFailures = digraph.MapNodes(_ => 0);
    // set up the time logging dictionaries
    var numNodes = Digraph.Nodes.Count;
    InferInitialTimes = new ConcurrentDictionary<TV, long>(_processes * 2, numNodes);
    InferInductiveTimes = new ConcurrentDictionary<TV, long>(_processes * 2, numNodes);
    InferBeforeInductiveTimes = new ConcurrentDictionary<(TV, IReadOnlyList<bool>), long>(_processes * 2, numNodes);
    InferAfterInductiveTimes = new ConcurrentDictionary<(TV, IReadOnlyList<bool>), long>(_processes * 2, numNodes);
  }

  public Infer(Network<T, TV, TS> net, IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> afterInvariants) : this(net.Digraph, net.TransferFunction,
    net.MergeFunction, net.InitialValues, beforeInvariants, afterInvariants, net.Symbolics)
  {
  }

  /// <summary>
  /// If true, log the times taken by inference to the Infer dictionaries.
  /// </summary>
  public bool LogInferenceTime { get; set; }

  /// <summary>
  /// If true, print the generated bounds to standard output.
  /// </summary>
  public bool PrintBounds { get; set; } = false;

  /// <summary>
  /// If true, report all failures to standard output.
  /// </summary>
  public bool ReportFailures { get; set; } = false;

  /// <summary>
  /// Record the number of inductive check failures for each node.
  /// </summary>
  private Dictionary<TV, int> NumInductiveFailures { get; }

  /// <summary>
  /// If true, report all times inferred to standard output.
  /// </summary>
  public bool PrintTimes { get; set; } = false;

  public BigInteger? MaxTime { get; set; }
  private IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> BeforeInvariants { get; }
  private IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> AfterInvariants { get; }

  /// <summary>
  ///   Return a string describing a failed check.
  ///   If b is not null, specify the b that caused the failure (inductive check);
  ///   otherwise, assume the failure is due to the initial check.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="invariantDescriptor">A descriptor of the invariant, e.g. "before" or "after".</param>
  /// <param name="b">An array of node names of the node's b, or null.</param>
  /// <returns>A string describing a failed check.</returns>
  private string ReportFailure(TV node, string invariantDescriptor, IReadOnlyList<bool>? b)
  {
    if (b is not null)
    {
      var bString = new StringBuilder();
      foreach (var i in Enumerable.Range(0, Digraph[node].Count))
      {
        if (bString.Length > 0) bString.Append(", ");
        // specify whether the neighbor was before or after
        bString.Append(b[i] ? "before " : "after ");
        bString.Append(Digraph[node][i]);
      }

      return $"Arrangement [{bString}] does NOT imply node {node}'s {invariantDescriptor} invariant.";
    }

    return $"Node {node}'s {invariantDescriptor} invariant does not hold for its initial route.";
  }

  private void PrintFailure(TV node, string invariantDescriptor, IReadOnlyList<bool>? b)
  {
    if (ReportFailures)
      Console.WriteLine(ReportFailure(node, invariantDescriptor, b));
  }

  /// <summary>
  ///   Check that a node's initial route satisfies the given invariant.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="invariant"></param>
  /// <returns>True if the invariant does *not* hold for the initial route, and false otherwise.</returns>
  private bool CheckInitial(TV node, Func<Zen<T>, Zen<bool>> invariant)
  {
    // TODO: move time logging here
    var query = Zen.And(GetSymbolicConstraints(), Zen.Not(invariant(InitialValues[node])));
    var model = query.Solve();
    return model.IsSatisfiable();
  }

  /// <summary>
  ///   Check that the given node's invariant is implied by the invariants of its neighbors.
  ///   The bitvector b controls whether the neighbor i sends a route satisfying its before condition
  ///   (b[i] is true) or after condition (b[i] is false)
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="invariant">A predicate to check on the node.</param>
  /// <param name="b">A bit array over the node's neighbors.</param>
  /// <param name="routes">The routes of the network for the neighboring nodes.</param>
  /// <param name="blockingClauses">An additional enumerable of clauses over b variables
  ///   to block when checking the invariant.</param>
  /// <returns>An arrangement b which causes the invariant to *not* always be satisfied if one exists, and null otherwise.</returns>
  private List<bool>? CheckInductive(TV node, Func<Zen<T>, Zen<bool>> invariant,
    IReadOnlyList<Zen<bool>?> b, IReadOnlyDictionary<TV, Zen<T>> routes,
    IEnumerable<Zen<bool>>? blockingClauses = null)
  {
    // TODO: move time logging here
    var newNodeRoute = UpdateNodeRoute(node, routes);

    // check predecessor invariants according to whether or not the predecessor was given in b
    // we check the before invariant of a predecessor when b[i] is true, and the after invariant when b[i] is false
    var assume = Digraph[node]
      .Select((predecessor, i) =>
        b[i] is null
          ? Zen.True() // skip nodes where b[i] is null
          : Zen.If(b[i], BeforeInvariants[predecessor](routes[predecessor]),
            AfterInvariants[predecessor](routes[predecessor])));
    var check = Zen.Implies(Zen.And(assume.ToArray()), invariant(newNodeRoute));

    var query = Zen.And(GetSymbolicConstraints(), blockingClauses is null
      ? Zen.Not(check)
      : Zen.And(Zen.And(blockingClauses), Zen.Not(check)));
    var model = query.Solve();

    return model.IsSatisfiable() ? b.Select(bi => model.Get(bi)).ToList() : null;
  }

  public Dictionary<TV, BigInteger> InferTimes(InferenceStrategy strategy)
  {
    var (beforeInitialChecks, afterInitialChecks) =
      FailingInitialChecks();
    // for each node, for each subset of its predecessors, run CheckInductive in parallel
    // construct a dictionary of the results of which b fail to imply the two invariants
    IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>> beforeInductiveChecks;
    IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>> afterInductiveChecks;
    switch (strategy)
    {
      case InferenceStrategy.ExplicitEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) =
          EnumerateArrangementsExplicit();
        break;
      case InferenceStrategy.SymbolicEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) =
          EnumerateArrangementsSymbolic();
        break;
      case InferenceStrategy.SelectiveEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) =
          EnumerateArrangementsSelective();
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
    }

    return InferTimesFromChecks(beforeInitialChecks, afterInitialChecks, beforeInductiveChecks, afterInductiveChecks);
  }

  /// <summary>
  /// Check all nodes' initial values against the invariants and return which nodes' invariants failed.
  /// </summary>
  /// <returns>Two collections collecting all nodes that failed the before invariant (the first collection),
  /// or the second invariant (the second collection).</returns>
  private (IReadOnlyCollection<TV>, IReadOnlyCollection<TV>) FailingInitialChecks()
  {
    var afterInitialChecks = new ConcurrentBag<TV>();
    var beforeInitialChecks = new ConcurrentBag<TV>();
    Digraph.Nodes
      // check all nodes' invariants in parallel
      .SelectMany(_ => new[] {true, false}, (n, b) => (n, b))
      .AsParallel().ForAll(tuple =>
      {
        var (node, b) = tuple;
        LogActionTime(node, InferInitialTimes, () =>
        {
          if (!CheckInitial(node, b ? BeforeInvariants[node] : AfterInvariants[node])) return;
          PrintFailure(node, b ? "before" : "after", null);
          (b ? beforeInitialChecks : afterInitialChecks).Add(node);
        });
      });
    return (beforeInitialChecks, afterInitialChecks);
  }

  /// <summary>
  ///   Explicitly enumerates the arrangements of before/after conditions of neighbors' routes.
  /// </summary>
  /// <returns>Two dictionaries mapping nodes to arrangements that fail the checks.</returns>
  private (IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>>, IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>>)
    EnumerateArrangementsExplicit()
  {
    var beforeInductiveChecks =
      new ConcurrentDictionary<TV, List<IReadOnlyList<bool>>>(_processes * 2, Digraph.Nodes.Count);
    var afterInductiveChecks =
      new ConcurrentDictionary<TV, List<IReadOnlyList<bool>>>(_processes * 2, Digraph.Nodes.Count);
    Digraph.Nodes
      // generate 2^{m+1} arrangements for each node
      .SelectMany(n => PowerSet.BitPSet(Digraph[n].Count + 1), (n, b) => (n, b))
      .AsParallel()
      .ForAll(tuple =>
      {
        var n = tuple.n;
        var before = tuple.b[^1];
        var b = tuple.b.Select(Zen.Constant).ToList();
        var routes = Digraph[n].ToDictionary(predecessor => predecessor, _ => Zen.Symbolic<T>());
        LogActionTime(tuple, InferBeforeInductiveTimes, () =>
        {
          if (CheckInductive(n, before ? BeforeInvariants[n] : AfterInvariants[n], b, routes) is null) return;
          NumInductiveFailures[n]++;
          PrintFailure(n, before ? "before" : "after", tuple.b);
          var arrangements =
            (before ? beforeInductiveChecks : afterInductiveChecks).GetOrAdd(n, new List<IReadOnlyList<bool>>());
          arrangements.Add(tuple.b);
        });
      });

    return (beforeInductiveChecks, afterInductiveChecks);
  }

  /// <summary>
  /// Return all arrangements for the given node that cause the inductive check to fail.
  /// Uses a solver to search for the arrangements, rather than explicitly enumerating them.
  /// </summary>
  /// <param name="node"></param>
  /// <returns></returns>
  private IEnumerable<IReadOnlyList<bool>> FindAllArrangements(TV node)
  {
    // keep track of the arrangements that fail
    var foundArrangements = new List<IReadOnlyList<bool>>();
    // generate an array of symbolic booleans of length equal to the node's predecessors + 1
    // each arrangement is a three-valued bitvector over the predecessors and the node
    var neighbors = Digraph[node].Count;
    var b = Enumerable.Range(0, neighbors + 1).Select(i => Zen.Symbolic<bool>($"b{i}")).ToList();
    var blockingClauses = new List<Zen<bool>>();
    var routes = Digraph[node].ToDictionary(neighbor => neighbor, _ => Zen.Symbolic<T>());
    var bSol = CheckInductive(node, r => Zen.If(b[^1], BeforeInvariants[node](r), AfterInvariants[node](r)), b, routes);
    while (bSol is not null)
    {
      // get the model and block it
      var concreteArrangement = bSol.ToList();
      PrintFailure(node, concreteArrangement[^1] ? "before" : "after", concreteArrangement);
      // construct a blocking clause: a negation of the case where all the B variables are as found by the solver
      var blockBs = bSol.Select((bb, i) => bb ? Zen.Not(b[i]) : b[i]);
      var blockingClause = Zen.Or(blockBs);
      blockingClauses.Add(blockingClause);
      // save this case as one to generate constraints for
      foundArrangements.Add(concreteArrangement);
      bSol =
        CheckInductive(node, r => Zen.If(b[^1], BeforeInvariants[node](r), AfterInvariants[node](r)), b, routes,
          blockingClauses: blockingClauses);
    }

    return foundArrangements;
  }

  private (IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>>, IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>>)
    EnumerateArrangementsSymbolic()
  {
    var beforeInductiveChecks =
      new ConcurrentDictionary<TV, List<IReadOnlyList<bool>>>(_processes * 2, Digraph.Nodes.Count);
    var afterInductiveChecks =
      new ConcurrentDictionary<TV, List<IReadOnlyList<bool>>>(_processes * 2, Digraph.Nodes.Count);
    Digraph.Nodes
      .AsParallel()
      .ForAll(node =>
      {
        LogActionTime(node, InferInductiveTimes, () =>
        {
          var arrangements = FindAllArrangements(node);
          foreach (var arr in arrangements)
          {
            // NOTE: could we simplify further and just use one dictionary?
            var failed =
              (arr[^1] ? beforeInductiveChecks : afterInductiveChecks).GetOrAdd(node, new List<IReadOnlyList<bool>>());
            failed.Add(arr);
            NumInductiveFailures[node]++;
          }
        });
      });
    return (beforeInductiveChecks, afterInductiveChecks);
  }

  private (IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>>, IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>>)
    EnumerateArrangementsSelective()
  {
    var beforeInductiveChecks = new Dictionary<TV, List<IReadOnlyList<bool>>>();
    var afterInductiveChecks = new Dictionary<TV, List<IReadOnlyList<bool>>>();

    var routes = Digraph.MapNodes(_ => Zen.Symbolic<T>());
    // Construct the "T1" table
    // for succinctness, we use a hashset to contain all the cases that pass
    var t1 = new Dictionary<(TV, bool), HashSet<(TV, bool)>>();
    // There are four combinations of before/after for each neighbor-node pair in the T1 table.
    var t1Combinations = new[] {(true, true), (true, false), (false, true), (false, false)};
    foreach (var (node, neighbors) in Digraph.Neighbors)
    {
      // Add the two new rows
      t1[(node, true)] = new HashSet<(TV, bool)>();
      t1[(node, false)] = new HashSet<(TV, bool)>();
      // Populate the rows for each neighbor ("column")
      for (var i = 0; i < neighbors.Count; i++)
      {
        var neighbor = neighbors[i];
        foreach (var (bu, bv) in t1Combinations)
        {
          var b = Enumerable.Range(0, neighbors.Count).Select(j => i == j ? Zen.Constant(bu) : null).ToList();
          // Check that the node's invariant holds given its initial route and the routes from only this neighbor.
          var check = CheckInductive(node, bv ? BeforeInvariants[node] : AfterInvariants[node],
            b, routes);
          // if the check passed, add the neighbor to the set
          if (check is null)
            t1[(node, bv)].Add((neighbor, bu));
        }
      }
    }

    // Construct the "T2" table
    // again, we use a hashset to represent which cases pass (included) or fail (excluded)
    var t2 = new Dictionary<(TV, bool), HashSet<(TV, bool, TV, bool)>>();
    // There are eight combinations of before/after for each neighbor-neighbor-node pair in the T1 table.
    var t2Combinations = new[]
    {
      (true, true, true), (true, false, true), (false, true, true), (false, false, true),
      (true, true, false), (true, false, false), (false, true, false), (false, false, false)
    };
    foreach (var (node, neighbors) in Digraph.Neighbors)
    {
      // graph representing which arrangements fail: start with no edges
      var failArrangementNeighbors = new Dictionary<(TV, bool), ImmutableSortedSet<(TV, bool)>>();
      foreach (var neighbor in neighbors)
      {
        failArrangementNeighbors.Add((neighbor, false), ImmutableSortedSet<(TV, bool)>.Empty);
        failArrangementNeighbors.Add((neighbor, true), ImmutableSortedSet<(TV, bool)>.Empty);
      }

      var failArrangementGraph = new EdgeLabelledDigraph<(TV, bool), bool?>(failArrangementNeighbors,
        new Dictionary<((TV, bool), (TV, bool)), bool?>());
      // add two new rows to the T2 table for this node
      t2[(node, true)] = new HashSet<(TV, bool, TV, bool)>();
      t2[(node, false)] = new HashSet<(TV, bool, TV, bool)>();
      // the inner neighbor loop starts at i1+1 as we don't want to visit the same neighbors again
      for (var i1 = 0; i1 < neighbors.Count; i1++)
      for (var i2 = i1 + 1; i2 < neighbors.Count; i2++)
      {
        var neighbor1 = neighbors[i1];
        var neighbor2 = neighbors[i2];
        foreach (var (b1, b2, bv) in t2Combinations)
        {
          if (!t1[(node, bv)].Contains((neighbor1, b1)) && !t1[(node, bv)].Contains((neighbor2, b2)))
          {
            // both neighbors fail
            // if the edge is already present, just update the label
            if (failArrangementGraph.Labels.ContainsKey(((neighbor1, b1), (neighbor2, b2))))
            {
              failArrangementGraph.Labels[((neighbor1, b1), (neighbor2, b2))] = null;
              failArrangementGraph.Labels[((neighbor2, b2), (neighbor1, b1))] = null;
            }
            else
            {
              failArrangementGraph.AddEdge((neighbor1, b1), (neighbor2, b2), bv);
              failArrangementGraph.AddEdge((neighbor2, b2), (neighbor1, b1), bv);
            }
          }
          else if (t1[(node, bv)].Contains((neighbor1, b1)) && t1[(node, bv)].Contains((neighbor2, b2)))
          {
            // both neighbors pass
            t2[(node, bv)].Add((neighbor1, b1, neighbor2, b2));
          }
          else
          {
            // set neighbors i1 and i2, and leave everything else null (skipped)
            var b = Enumerable.Range(0, neighbors.Count).Select(j => i1 == j ? Zen.Constant(b1) :
              i2 == j ? Zen.Constant(b2) : null).ToList();
            // Check that the node's invariant holds given its initial route and the routes from only these two neighbors.
            var check = CheckInductive(node, bv ? BeforeInvariants[node] : AfterInvariants[node], b, routes);
            if (check is null)
            {
              // pair passed, add to t2
              t2[(node, bv)].Add((neighbor1, b1, neighbor2, b2));
            }
            else
            {
              // if the edge is already present, just update the label
              if (failArrangementGraph.Labels.ContainsKey(((neighbor1, b1), (neighbor2, b2))))
              {
                failArrangementGraph.Labels[((neighbor1, b1), (neighbor2, b2))] = null;
                failArrangementGraph.Labels[((neighbor2, b2), (neighbor1, b1))] = null;
              }
              else
              {
                failArrangementGraph.AddEdge((neighbor1, b1), (neighbor2, b2), bv);
                failArrangementGraph.AddEdge((neighbor2, b2), (neighbor1, b1), bv);
              }
            }
          }
        }
      }

      // TODO: remove this and replace with the fixed version below
      // reconstruct the result for every arrangement
      // foreach (var (neighbor, inv) in failArrangementGraph.Nodes)
      // {
      // var (beforeComponent, afterComponent) = ReachableNodes(failArrangementGraph, neighbor, inv);
      // add the arrangements to the appropriate dictionaries
      // if (beforeComponent.Count == Digraph[node].Count)
      // {
      // if (!beforeInductiveChecks.ContainsKey(node)) beforeInductiveChecks[node] = new List<IReadOnlyList<bool>>();
      // beforeInductiveChecks[node].Add(Digraph[node].Select(n => beforeComponent[n]).ToList());
      // }

      // if (afterComponent.Count == Digraph[node].Count)
      // {
      // if (!afterInductiveChecks.ContainsKey(node)) afterInductiveChecks[node] = new List<IReadOnlyList<bool>>();
      // afterInductiveChecks[node].Add(Digraph[node].Select(n => afterComponent[n]).ToList());
      // }
      // }
    }

    // TODO: perhaps we could filter out any arrangements that we already know will pass?
    Digraph.Nodes
      .SelectMany(n => PowerSet.BitPSet(Digraph[n].Count + 1), (n, b) => (n, b))
      .AsParallel()
      .ForAll(tuple =>
      {
        var (node, b) = tuple;
        // reconstruct this arrangement's pass/fail
        // for the nodes in this arrangement, see if the relevant clique connects all the components
        var before = b[^1];
        // based on the value of before, we want to check if the complement graph of the edges in t2[(node, bv)]
        // is a clique? visits every vertex?
        // possible dumb way: try every "edge" in succession
        // is this like a DFS?
        var reached = ArrangementReachable(b.Count, (node, before), t2, ImmutableSortedSet<TV>.Empty);
        for (var i = 0; i < b.Count - 1; i++)
        {
          if (t2[(node, before)].Contains((Digraph[node][i], b[i], Digraph[node][i + 1], b[i + 1])))
          {
            // there is an edge from i to i+1
            // TODO: do something lol
          }
        }
      });
    return (beforeInductiveChecks, afterInductiveChecks);
  }

  /// <summary>
  /// Return the nodes reached from the given starting invariant.
  /// </summary>
  /// <param name="fuel"></param>
  /// <param name="start"></param>
  /// <param name="t2"></param>
  /// <param name="reached"></param>
  /// <returns></returns>
  private static ImmutableSortedSet<TV> ArrangementReachable(int fuel, (TV, bool) start,
    IReadOnlyDictionary<(TV, bool), HashSet<(TV, bool, TV, bool)>> t2, ImmutableSortedSet<TV> reached)
  {
    reached = reached.Add(start.Item1);
    // if we run out of fuel, abort with an empty set
    return fuel <= 0
      ? ImmutableSortedSet<TV>.Empty
      : t2[start].Where(m => m.Item1.Equals(start.Item1) && m.Item2 == start.Item2 && !reached.Contains(m.Item3))
        .Aggregate(reached,
          (current, m) => current.Union(ArrangementReachable(fuel - 1, (m.Item3, m.Item4), t2, current)));
  }

  /// <summary>
  /// Construct two dictionaries capturing the nodes reachable from the given starting (node, inv) pair
  /// in the graph.
  /// The first dictionary contains the nodes that are reachable along edges labelled true,
  /// while the second dictionary contains the nodes that are reachable along edges labelled false.
  /// Effectively we are doing a modified backward BFS.
  /// </summary>
  /// <param name="g"></param>
  /// <param name="node"></param>
  /// <param name="inv"></param>
  /// <returns></returns>
  private static (IDictionary<TV, bool>, IDictionary<TV, bool>) ReachableNodes(EdgeLabelledDigraph<(TV, bool), bool?> g,
    TV node, bool inv)
  {
    var q = new Queue<(TV, bool)>();
    // nodes reachable along edges labelled true
    var beforeReached = ImmutableSortedDictionary.Create<TV, bool>().Add(node, inv);
    // nodes reachable along edges labelled false
    var afterReached = ImmutableSortedDictionary.Create<TV, bool>().Add(node, inv);
    q.Enqueue((node, inv));
    while (q.Count > 0)
    {
      var n = q.Dequeue();
      // we skip neighbors that are already in both sets,
      // or already in both sets as the node's other phase ("repeating colors")
      foreach (var m in g.Neighbors[n]
                 .Where(m => !beforeReached.Any(br => br.Key.Equals(m.Item1))
                             || !afterReached.Any(ar => ar.Key.Equals(m.Item1))))
      {
        if (g.Labels[(m, n)] is not false)
          beforeReached = beforeReached.Add(m.Item1, m.Item2);
        if (g.Labels[(m, n)] is not true)
          afterReached = afterReached.Add(m.Item1, m.Item2);
        q.Enqueue(m);
      }
    }

    return (beforeReached, afterReached);
  }

  /// <summary>
  /// Return a dictionary from nodes to witness times, such that the returned witness times
  /// ensure that all the given arrangements are blocked, or an empty dictionary if no such witness times exist.
  /// </summary>
  /// <param name="beforeInitialChecks">Nodes where the before invariant failed to hold on the initial value.</param>
  /// <param name="afterInitialChecks">Nodes where the after invariant failed to hold on the initial value.</param>
  /// <param name="beforeInductiveChecks">Node arrangements where the inductive check failed with the before invariant.</param>
  /// <param name="afterInductiveChecks">Node arrangements where the inductive check failed with the after invariant.</param>
  /// <returns></returns>
  private Dictionary<TV, BigInteger> InferTimesFromChecks(IEnumerable<TV> beforeInitialChecks,
    IEnumerable<TV> afterInitialChecks, IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>> beforeInductiveChecks,
    IReadOnlyDictionary<TV, List<IReadOnlyList<bool>>> afterInductiveChecks)
  {
    // TODO: if we have check failures when predecessor u is both in b and not in b,
    // TODO: then we should exclude it from the generated bounds (since its value won't matter)
    var times = Digraph.MapNodes(node => Zen.Symbolic<BigInteger>($"{node}-time"));
    // enforce that all times must be non-negative
    var bounds = times.Select(t => t.Value >= BigInteger.Zero).ToImmutableHashSet();
    // if a maximum time is given, also require that no witness time is greater than the maximum
    if (MaxTime is not null) bounds = bounds.Union(times.Select(pair => pair.Value <= MaxTime));

    // add initial check bounds
    bounds = bounds.Union(
      beforeInitialChecks.Select<TV, Zen<bool>>(node => times[node] == BigInteger.Zero)
        .Concat(afterInitialChecks.Select<TV, Zen<bool>>(node => times[node] > BigInteger.Zero)));

    // var simplifiedBeforeInductiveChecks = beforeInductiveChecks.Select(p =>
    // new KeyValuePair<TV, IEnumerable<bool?[]>>(p.Key, PrimeArrangements.SimplifyArrangements(p.Value)));
    // var simplifiedAfterInductiveChecks = afterInductiveChecks.Select(p =>
    // new KeyValuePair<TV, IEnumerable<bool?[]>>(p.Key, PrimeArrangements.SimplifyArrangements(p.Value)));
    // for each failed inductive check, we add the following bounds:
    // (1) if the before check failed for node n and b anc, add bounds for all b m in anc
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 >= t_n
    foreach (var (node, arrangements) in beforeInductiveChecks)
    {
      bounds = (from arrangement in arrangements select BoundArrangement(node, times, arrangement, true)).Aggregate(
        bounds, (current, beforeBounds) => current.Union(beforeBounds));
    }

    // (2) if the after check failed for node n and b anc, add bounds for all predecessors m of n
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 < t_n,
    //     or n converges before all nodes u_j in anc (t_n - 1 < t_j), or after all nodes u_j not in anc (t_n - 1 >= t_j)
    foreach (var (node, arrangements) in afterInductiveChecks)
    foreach (var arrangement in arrangements)
    {
      var nextBound = TimeInterval(Digraph[node], times[node] - BigInteger.One, times, arrangement);
      // var (earlierNeighbors, laterNeighbors) = PartitionNeighborsByArrangement(node, arrangement);
      // var nextBound = TimeInterval(earlierNeighbors, laterNeighbors, times[node] - BigInteger.One, times);
      bounds = bounds.Add(nextBound)
        .Union(BoundArrangement(node, times, arrangement, false));
    }

    // print the computed bounds
    if (PrintBounds)
      foreach (var b in bounds)
        Console.WriteLine(b);

    var model = Zen.And(bounds).Solve();
    if (model.IsSatisfiable())
      return new Dictionary<TV, BigInteger>(times.Select(pair =>
        new KeyValuePair<TV, BigInteger>(pair.Key, model.Get(pair.Value))));
    return new Dictionary<TV, BigInteger>();
  }

  /// <summary>
  ///   Generate a disjunction of constraints on the given time: for the given predecessors and time,
  ///   the constraints require that each of the predecessor's times is at most the given time
  ///   if the predecessor is set in the arrangement, and otherwise strictly greater than the given time.
  /// </summary>
  /// <param name="predecessors">The predecessor nodes.</param>
  /// <param name="time">The symbolic time.</param>
  /// <param name="times">The witness times of the predecessors.</param>
  /// <param name="arrangement">
  /// The arrangement of the predecessors, such that if arrangement[i] is true for predecessor i,
  /// then the given symbolic time is greater than or equal to predecessor i's witness time,
  /// and otherwise less than (when arrangement[i] is false).</param>
  /// <returns>A disjunction over comparisons between time and the witness times.</returns>
  private static Zen<bool> TimeInterval(IEnumerable<TV> predecessors, Zen<BigInteger> time,
    IReadOnlyDictionary<TV, Zen<BigInteger>> times, IReadOnlyList<bool> arrangement)
  {
    var neighborBounds = predecessors
      // skip cases where the symbolic time is for the given predecessor,
      // but would be set to be less than itself
      // (arrangement is false and the symbolic time is equal to the predecessor's time)
      // .Where((j, i) => arrangement[i] || !time.Equals(times[j]))
      .Select((j, i) => arrangement[i] ? time >= times[j] : time < times[j]).ToArray();
    return neighborBounds.Length > 0 ? Zen.Or(neighborBounds) : false;
  }

  private static Zen<bool> TimeInterval(IEnumerable<TV> earlierNeighbors,
    IEnumerable<TV> laterNeighbors,
    Zen<BigInteger> time,
    IReadOnlyDictionary<TV, Zen<BigInteger>> times)
  {
    var neighborBounds = earlierNeighbors.Select(en => time >= times[en])
      .Concat(laterNeighbors.Select(ln => time < times[ln])).ToArray();
    return neighborBounds.Length > 0 ? Zen.Or(neighborBounds) : false;
  }

  /// <summary>
  /// Return an enumerable of boolean constraints representing that
  /// there does not exist a time such that the given arrangement can occur.
  /// Each constraint captures a possible lower bound on an interval,
  /// such that all possible times that could occur are considered,
  /// effectively eliminating the quantifier over time.
  /// </summary>
  /// <param name="node">The node for which the arrangement is being considered.</param>
  /// <param name="times">The witness times of the node and its predecessors.</param>
  /// <param name="arrangement">A List{bool} representing the neighbors and node, in order (neighbors then node).</param>
  /// <param name="before">True if the bound is for the given node's before invariant, and false for its after invariant.</param>
  /// <returns>An enumerable of constraints.</returns>
  private IEnumerable<Zen<bool>> BoundArrangement(TV node,
    IReadOnlyDictionary<TV, Zen<BigInteger>> times, IReadOnlyList<bool> arrangement, bool before)
  {
    // the instantiated bounds are 0 and all neighbors that have already converged (the arrangement is false at the neighbor)
    var lowerBounds = Enumerable.Repeat(Zen.Constant(BigInteger.Zero), 1)
      .Concat(from neighbor in Digraph[node].Where((_, i) => !arrangement[i]) // indexed where has no query syntax form
        select times[neighbor]);
    // for each lower bound, add a disjunction that rules out the case
    return from lowerBound in lowerBounds
      select Zen.Or(
        before ? lowerBound + BigInteger.One >= times[node] : lowerBound + BigInteger.One < times[node],
        TimeInterval(Digraph[node], lowerBound, times, arrangement));
  }

  private (List<TV>, List<TV>) PartitionNeighborsByArrangement(TV node, IReadOnlyList<bool?> arrangement)
  {
    var earlierNeighbors = new List<TV>();
    var laterNeighbors = new List<TV>();
    for (var i = 0; i < Digraph[node].Count; i++)
    {
      if (arrangement[i] is null) continue;
      if ((bool) arrangement[i]!)
      {
        earlierNeighbors.Add(Digraph[node][i]);
      }
      else
      {
        laterNeighbors.Add(Digraph[node][i]);
      }
    }

    return (earlierNeighbors, laterNeighbors);
  }

  private IEnumerable<Zen<bool>> BoundArrangement(TV node, IReadOnlyDictionary<TV, Zen<BigInteger>> times,
    IReadOnlyList<bool?> arrangement)
  {
    var (earlierNeighbors, laterNeighbors) = PartitionNeighborsByArrangement(node, arrangement);
    var lowerBounds = Enumerable.Repeat(Zen.Constant(BigInteger.Zero), 1)
      .Concat(laterNeighbors.Select(n => times[n]));
    return from lowerBound in lowerBounds
      select Zen.Or(
        // if arrangement[^1] is null, then we can ignore it entirely
        arrangement[^1] is null
          ? false
          : (bool) arrangement[^1]!
            ? lowerBound + BigInteger.One >= times[node]
            : lowerBound + BigInteger.One < times[node],
        TimeInterval(earlierNeighbors, laterNeighbors, times[node], times));
  }

  public static (long, Dictionary<TV, BigInteger>) Time(Func<Dictionary<TV, BigInteger>> inferFunc)
  {
    var timer = Stopwatch.StartNew();
    var times = inferFunc();
    return (timer.ElapsedMilliseconds, times);
  }

  /// <summary>
  /// If logging is on, log the time taken by the given action to complete.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="times"></param>
  /// <param name="inferAction"></param>
  /// <typeparam name="TKey"></typeparam>
  public void LogActionTime<TKey>(TKey key, IDictionary<TKey, long> times, Action inferAction)
  {
    if (LogInferenceTime)
    {
      var timer = Stopwatch.StartNew();
      inferAction();
      times.Add(key, timer.ElapsedMilliseconds);
    }
    else
    {
      inferAction();
    }
  }

  /// <summary>
  /// Infer suitable annotations for a Timepiece <c>AnnotatedNetwork{T,TS}</c> instance.
  /// The given inference strategy determines the form of inference used.
  /// </summary>
  /// <param name="strategy">the <c>InferenceStrategy</c> inference strategy: see <see cref="InferenceStrategy"/></param>
  /// <exception cref="ArgumentOutOfRangeException">if an invalid inference strategy is given</exception>
  /// <returns>a dictionary from nodes to temporal predicates</returns>
  public Dictionary<TV, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> InferAnnotationsWithStats(InferenceStrategy strategy)
  {
    LogInferenceTime = true;
    try
    {
      Console.WriteLine("Inferring witness times...");
      var (timeTaken, witnessTimes) = Time(() =>
        InferTimes(strategy));
      Console.WriteLine($"Inference took {timeTaken}ms!");

      if (witnessTimes.Count > 0)
      {
        if (PrintTimes)
        {
          Console.WriteLine("Success, inferred the following times:");
          foreach (var (node, time) in witnessTimes) Console.WriteLine($"{node}: {time}");
        }

        return Digraph.MapNodes(n => Lang.Until(witnessTimes[n], BeforeInvariants[n], AfterInvariants[n]));
      }
    }
    catch (ZenException e)
    {
      Console.WriteLine("Error, inference did not complete:");
      Console.WriteLine(e.Message);
    }
    finally
    {
      Console.WriteLine("Initial statistics:");
      StatisticsExtensions.ReportTimes(InferInitialTimes, Statistics.Summary, null, false);
      Console.WriteLine("Inductive failure statistics:");
      FiveNumberSummary(NumInductiveFailures);
      switch (strategy)
      {
        case InferenceStrategy.ExplicitEnumeration:
          Console.WriteLine("Before inductive statistics:");
          StatisticsExtensions.ReportTimes(InferBeforeInductiveTimes, Statistics.Summary, null, false);
          Console.WriteLine("After inductive statistics:");
          StatisticsExtensions.ReportTimes(InferAfterInductiveTimes, Statistics.Summary, null, false);
          break;
        case InferenceStrategy.SymbolicEnumeration:
          Console.WriteLine("Inductive statistics:");
          StatisticsExtensions.ReportTimes(InferInductiveTimes, Statistics.Summary, null, false);
          break;
      }
    }

    throw new ArgumentException("Failed to infer times!");
  }

  private static void FiveNumberSummary(IDictionary<TV, int> d)
  {
    var len = d.Count;
    var ordered = d.OrderBy(p => p.Value).ToArray();
    var (minNode, min) = ordered[0];
    var (maxNode, max) = ordered[^1];
    var (medNode, med) = ordered[len / 2];
    var (lowerNode, lower) = ordered[len / 4];
    var (upperNode, upper) = ordered[3 * len / 4];
    Console.WriteLine($"Minimum node {minNode}: {min}");
    Console.WriteLine($"Lower-quantile node {lowerNode}: {lower}");
    Console.WriteLine($"Median node {medNode}: {med}");
    Console.WriteLine($"Upper-quantile node {upperNode}: {upper}");
    Console.WriteLine($"Maximum node {maxNode}: {max}");
  }
}

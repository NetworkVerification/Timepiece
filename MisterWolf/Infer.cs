using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Timepiece;
using Timepiece.Networks;
using ZenLib;

namespace MisterWolf;

public class Infer<TRoute, TNode, TSymbolic> : Network<TRoute, TNode, TSymbolic> where TNode : IEquatable<TNode>
{
  private readonly int _processes = Environment.ProcessorCount;
  private ConcurrentDictionary<TNode, long> InferInitialTimes { get; set; }
  private ConcurrentDictionary<TNode, long> InferInductiveTimes { get; set; }
  private ConcurrentDictionary<(TNode, IReadOnlyList<bool>), long> InferBeforeInductiveTimes { get; set; }
  private ConcurrentDictionary<(TNode, IReadOnlyList<bool>), long> InferAfterInductiveTimes { get; set; }

  public Infer(Digraph<TNode> digraph,
    Dictionary<(TNode, TNode), Func<Zen<TRoute>, Zen<TRoute>>> transferFunction,
    Func<Zen<TRoute>, Zen<TRoute>, Zen<TRoute>> mergeFunction,
    Dictionary<TNode, Zen<TRoute>> initialValues,
    IReadOnlyDictionary<TNode, Func<Zen<TRoute>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TNode, Func<Zen<TRoute>, Zen<bool>>> afterInvariants,
    SymbolicValue<TSymbolic>[] symbolics) : base(digraph, transferFunction, mergeFunction,
    initialValues, symbolics)
  {
    BeforeInvariants = beforeInvariants;
    AfterInvariants = afterInvariants;
    NumInductiveFailures = digraph.MapNodes(_ => 0);
    // set up the time logging dictionaries
    var numNodes = Digraph.Nodes.Count;
    InferInitialTimes = new ConcurrentDictionary<TNode, long>(_processes * 2, numNodes);
    InferInductiveTimes = new ConcurrentDictionary<TNode, long>(_processes * 2, numNodes);
    InferBeforeInductiveTimes = new ConcurrentDictionary<(TNode, IReadOnlyList<bool>), long>(_processes * 2, numNodes);
    InferAfterInductiveTimes = new ConcurrentDictionary<(TNode, IReadOnlyList<bool>), long>(_processes * 2, numNodes);
  }

  public Infer(Network<TRoute, TNode, TSymbolic> net,
    IReadOnlyDictionary<TNode, Func<Zen<TRoute>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TNode, Func<Zen<TRoute>, Zen<bool>>> afterInvariants) : this(net.Digraph, net.TransferFunction,
    net.MergeFunction, net.InitialValues, beforeInvariants, afterInvariants, net.Symbolics)
  {
  }

  /// <summary>
  /// If true, log the times taken by inference to the Infer dictionaries.
  /// </summary>
  public bool LogTimes { get; set; }

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
  private Dictionary<TNode, int> NumInductiveFailures { get; }

  /// <summary>
  /// If true, report all times inferred to standard output.
  /// </summary>
  public bool PrintTimes { get; set; } = false;

  public BigInteger? MaxTime { get; init; }
  private IReadOnlyDictionary<TNode, Func<Zen<TRoute>, Zen<bool>>> BeforeInvariants { get; }
  private IReadOnlyDictionary<TNode, Func<Zen<TRoute>, Zen<bool>>> AfterInvariants { get; }

  private static string BoolToInvariantType(bool b) => b ? "before" : "after";

  private static string ArrangementToString(IEnumerable<bool> b, IEnumerable<TNode> neighbors)
  {
    var builder = new StringBuilder();
    foreach (var (neighbor, bb) in neighbors.Zip(b))
    {
      if (builder.Length > 0)
      {
        builder.Append("/");
      }

      builder.Append($"{neighbor}:{BoolToInvariantType(bb)}");
    }

    return builder.ToString();
  }

  /// <summary>
  ///   Return a string describing a failed check.
  ///   If b is not null, specify the b that caused the failure (inductive check);
  ///   otherwise, assume the failure is due to the initial check.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="neighbors">The neighbors of the node.</param>
  /// <param name="bv">A nullable boolean representing the node's invariant.</param>
  /// <param name="b">An arrangement representing the neighbors' invariants.</param>
  /// <returns>A string describing a failed check.</returns>
  private static string ReportFailure(TNode node, IEnumerable<TNode>? neighbors, bool bv, List<bool>? b)
  {
    return b is not null && neighbors is not null
      ? $"Arrangement [{ArrangementToString(b, neighbors)}:{node}] failed."
      : $"Node {node}'s {BoolToInvariantType(bv)} invariant does not hold for its initial route.";
  }

  private void PrintFailure(TNode node, IEnumerable<TNode>? neighbors, bool bv, List<bool>? b)
  {
    if (ReportFailures)
      Console.WriteLine(ReportFailure(node, neighbors, bv, b));
  }

  /// <summary>
  ///   Check that a node's initial route satisfies the given invariant.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="invariant"></param>
  /// <returns>True if the invariant does *not* hold for the initial route, and false otherwise.</returns>
  private bool CheckInitial(TNode node, Func<Zen<TRoute>, Zen<bool>> invariant)
  {
    return LogFunctionTime(node, InferInitialTimes, () =>
    {
      var query = Zen.And(GetSymbolicConstraints(), Zen.Not(invariant(InitialValues[node])));
      var model = query.Solve();
      return model.IsSatisfiable();
    });
  }

  /// <summary>
  ///   Check that the given node's invariant is implied by the invariants of its neighbors.
  ///   The bitvector b controls whether the neighbor i sends a route satisfying its before condition
  ///   (b[i] is true) or after condition (b[i] is false)
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="neighbors">The specific neighbors to consider of the node.</param>
  /// <param name="invariant">A predicate to check on the node.</param>
  /// <param name="b">A bit array over the node's neighbors.</param>
  /// <param name="routes">The routes of the network for the neighboring nodes.</param>
  /// <param name="blockingClauses">An additional enumerable of clauses over b variables
  ///   to block when checking the invariant.</param>
  /// <returns>An arrangement b which causes the invariant to *not* always be satisfied if one exists, and null otherwise.</returns>
  private List<bool>? CheckInductive(TNode node, IEnumerable<TNode> neighbors, Func<Zen<TRoute>, Zen<bool>> invariant,
    IReadOnlyList<Zen<bool>> b, IReadOnlyDictionary<TNode, Zen<TRoute>> routes,
    IEnumerable<Zen<bool>>? blockingClauses = null)
  {
    return LogFunctionTime(node, InferInductiveTimes, () =>
    {
      var nbrs = neighbors.ToArray();
      var newNodeRoute = UpdateNodeRoute(node, routes, nbrs);

      // check predecessor invariants according to whether or not the predecessor was given in b
      // we check the before invariant of a predecessor when b[i] is true, and the after invariant when b[i] is false
      var assume = nbrs
        .Select((predecessor, i) =>
          Zen.If(b[i], BeforeInvariants[predecessor](routes[predecessor]),
            AfterInvariants[predecessor](routes[predecessor])));
      var check = Zen.Implies(Zen.And(assume.ToArray()), invariant(newNodeRoute));

      var query = Zen.And(GetSymbolicConstraints(), blockingClauses is null
        ? Zen.Not(check)
        : Zen.And(Zen.And(blockingClauses), Zen.Not(check)));
      var model = query.Solve();

      return model.IsSatisfiable() ? b.Select(model.Get).ToList() : null;
    });
  }

  public Dictionary<TNode, BigInteger> InferTimes(InferenceStrategy strategy)
  {
    var initialChecks = FailingInitialChecks();
    // for each node, for each subset of its predecessors, run CheckInductive in parallel
    // construct a dictionary of the results of which b fail to imply the two invariants
    var inductiveChecks = strategy switch
    {
      InferenceStrategy.ExplicitEnumeration => EnumerateArrangementsExplicit(),
      InferenceStrategy.SymbolicEnumeration => EnumerateArrangementsSymbolic(),
      InferenceStrategy.SelectiveEnumeration => EnumerateArrangementsSelective(),
      _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
    };

    return InferTimesFromChecks(initialChecks, inductiveChecks);
  }

  /// <summary>
  /// Check all nodes' initial values against the invariants and return which nodes' invariants failed.
  /// </summary>
  /// <returns>Two collections collecting all nodes that failed the before invariant (the first collection),
  /// or the second invariant (the second collection).</returns>
  private IEnumerable<(TNode, bool)> FailingInitialChecks()
  {
    return Digraph.Nodes
      .SelectMany(_ => new[] {true, false}, (n, b) => (n, b))
      // check all nodes' invariants in parallel
      .AsParallel().Aggregate(new ConcurrentBag<(TNode, bool)>(), (initialChecks, tuple) =>
      {
        var (node, b) = tuple;
        if (CheckInitial(node, b ? BeforeInvariants[node] : AfterInvariants[node]))
        {
          PrintFailure(node, null, b, null);
          initialChecks.Add(tuple);
        }

        return initialChecks;
      });
  }

  /// <summary>
  ///   Explicitly enumerates the arrangements of before/after conditions of neighbors' routes.
  /// </summary>
  /// <returns>Two dictionaries mapping nodes to arrangements that fail the checks.</returns>
  private IReadOnlyDictionary<TNode, List<IReadOnlyList<bool>>> EnumerateArrangementsExplicit()
  {
    return Digraph.Nodes
      // generate 2^{m+1} arrangements for each node
      .SelectMany(n => PowerSet.BitPSet(Digraph[n].Count + 1),
        (n, b) => (n, b))
      .AsParallel()
      .Aggregate(new ConcurrentDictionary<TNode, List<IReadOnlyList<bool>>>(_processes * 2, Digraph.Nodes.Count),
        (inductiveChecks, tuple) =>
        {
          var (n, arrangement) = tuple;
          var routes = Digraph[n].ToDictionary(predecessor => predecessor, _ => Zen.Symbolic<TRoute>());
          var check = CheckInductive(n, Digraph[n], arrangement[^1] ? BeforeInvariants[n] : AfterInvariants[n],
            arrangement.Select(Zen.Constant).ToList(), routes);
          if (check is not null)
          {
            NumInductiveFailures[n]++;
            PrintFailure(n, Digraph[n], arrangement[^1], check);
            var arrangements = inductiveChecks.GetOrAdd(n, new List<IReadOnlyList<bool>>());
            arrangements.Add(check);
          }

          return inductiveChecks;
        });
  }

  /// <summary>
  /// Return all arrangements for the given node that cause the inductive check to fail.
  /// Uses a solver to search for the arrangements, rather than explicitly enumerating them.
  /// </summary>
  /// <param name="node"></param>
  /// <returns></returns>
  private IEnumerable<IReadOnlyList<bool>> FindAllArrangements(TNode node)
  {
    // keep track of the arrangements that fail
    var foundArrangements = new List<IReadOnlyList<bool>>();
    // generate an array of symbolic booleans of length equal to the node's predecessors + 1
    // each arrangement is a three-valued bitvector over the predecessors and the node
    // var b = Enumerable.Range(0, neighbors + 1).Select(i => Zen.Symbolic<bool>($"b{i}")).ToList();
    // var b = ArrangementExtensions.Symbolic(node);
    // var b = Zen.Create<CMapList<Option<bool>>>(("Invariant", Zen.Symbolic<Option<bool>>()),
    // ("Neighbors",
    // Digraph[node].Aggregate(CMap.Empty<TNode, Option<bool>>(),
    // (map, neighbor) => map.Set(neighbor, Zen.Symbolic<Option<bool>>()))));
    var b = Enumerable.Repeat(Zen.Symbolic<bool>(), Digraph[node].Count + 1).ToList();
    var blockingClauses = new List<Zen<bool>>();
    var routes = Digraph[node].ToDictionary(neighbor => neighbor, _ => Zen.Symbolic<TRoute>());
    var bSol = CheckInductive(node, Digraph[node],
      r => Zen.If(b[^1], BeforeInvariants[node](r), AfterInvariants[node](r)), b, routes);
    while (bSol is not null)
    {
      // get the model and block it
      var concreteArrangement = bSol;
      PrintFailure(node, Digraph[node], concreteArrangement[^1], concreteArrangement);
      // construct a blocking clause: a negation of the case where all the B variables are as found by the solver
      // skip any b's that are null
      var blockingClause = Zen.Or(Digraph[node].Select((_, i) => Zen.Not(b[i])));
      blockingClauses.Add(blockingClause);
      // save this case as one to generate constraints for
      foundArrangements.Add(concreteArrangement);
      bSol =
        CheckInductive(node, Digraph[node], r => Zen.If(b[^1], BeforeInvariants[node](r), AfterInvariants[node](r)),
          b, routes,
          blockingClauses: blockingClauses);
    }

    return foundArrangements;
  }

  private IReadOnlyDictionary<TNode, List<IReadOnlyList<bool>>> EnumerateArrangementsSymbolic()
  {
    var inductiveChecks =
      new ConcurrentDictionary<TNode, List<IReadOnlyList<bool>>>(_processes * 2, Digraph.Nodes.Count);
    Digraph.Nodes
      .AsParallel()
      .ForAll(node =>
      {
        var arrangements = FindAllArrangements(node);
        foreach (var arr in arrangements)
        {
          var failed = inductiveChecks.GetOrAdd(node, new List<IReadOnlyList<bool>>());
          failed.Add(arr);
          NumInductiveFailures[node]++;
        }
      });
    return inductiveChecks;
  }

  private IReadOnlyDictionary<TNode, List<IReadOnlyList<bool>>> EnumerateArrangementsSelective()
  {
    var inductiveChecks = new ConcurrentDictionary<TNode, List<IReadOnlyList<bool>>>();

    var routes = Digraph.MapNodes(_ => Zen.Symbolic<TRoute>());
    // Construct the "T1" table
    // for succinctness, we use a hashset to contain all the cases that pass
    // TODO: add merging: if the HashSet contains both (m, true) and (m, false), we replace it with (m, null)
    var t1 = new Dictionary<(TNode, bool), HashSet<(TNode, bool)>>();
    // There are four combinations of before/after for each neighbor-node pair in the T1 table.
    var t1Combinations = new[] {(true, true), (true, false), (false, true), (false, false)};
    foreach (var (node, neighbors) in Digraph.Neighbors)
    {
      // Add the two new rows
      t1[(node, true)] = new HashSet<(TNode, bool)>();
      t1[(node, false)] = new HashSet<(TNode, bool)>();
      // Populate the rows for each neighbor ("column")
      foreach (var neighbor in neighbors)
      {
        foreach (var (bu, bv) in t1Combinations)
        {
          // Check that the node's invariant holds given its initial route and the routes from only this neighbor.
          var check = CheckInductive(node, new[] {neighbor}, bv ? BeforeInvariants[node] : AfterInvariants[node],
            new[] {Zen.Constant(bu)}, routes);
          // if the check passed, add the neighbor to the set
          if (check is null)
            t1[(node, bv)].Add((neighbor, bu));
        }
      }
    }

    // Construct the "T2" table
    // again, we use a hashset to represent which cases pass (included) or fail (excluded)
    var t2 = new Dictionary<(TNode, bool), HashSet<(TNode, bool, TNode, bool)>>();
    // There are eight combinations of before/after for each neighbor-neighbor-node pair in the T1 table.
    var t2Combinations = new[]
    {
      (true, true, true), (true, false, true), (false, true, true), (false, false, true),
      (true, true, false), (true, false, false), (false, true, false), (false, false, false)
    };
    foreach (var (node, neighbors) in Digraph.Neighbors)
    {
      // add two new rows to the T2 table for this node
      t2[(node, true)] = new HashSet<(TNode, bool, TNode, bool)>();
      t2[(node, false)] = new HashSet<(TNode, bool, TNode, bool)>();
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
          }
          else if (t1[(node, bv)].Contains((neighbor1, b1)) && t1[(node, bv)].Contains((neighbor2, b2)))
          {
            // both neighbors pass
            t2[(node, bv)].Add((neighbor1, b1, neighbor2, b2));
          }
          else
          {
            // Check that the node's invariant holds given its initial route and the routes from only these two neighbors.
            var check = CheckInductive(node, new[] {neighbor1, neighbor2},
              bv ? BeforeInvariants[node] : AfterInvariants[node], new[] {Zen.Constant(b1), Zen.Constant(b2)}, routes);
            if (check is null)
            {
              // pair passed, add to t2
              t2[(node, bv)].Add((neighbor1, b1, neighbor2, b2));
            }
          }
        }
      }
    }

    // TODO: perhaps we could filter out any arrangements that we already know will pass?
    Digraph.Nodes
      .SelectMany(n => PowerSet.BitPSet(Digraph[n].Count + 1),
        (n, b) => (n, b))
      .AsParallel()
      .ForAll(tuple =>
      {
        // reconstruct this arrangement's pass/fail
        var (node, arrangement) = tuple;
        var before = arrangement[^1];
        // check if the arrangement is connected
        // (every node in the arrangement is connected to some other node in the arrangement)
        var connected = true;
        // since the neighbors are added to T2 in ascending order, we can check them again in ascending order
        for (var i = 0; i < Digraph[node].Count; i++)
        for (var j = i + 1; j < Digraph[node].Count; j++)
        {
          connected &= t2[(node, before)]
            .Contains((Digraph[node][i], arrangement[i], Digraph[node][j], arrangement[j]));
        }

        // if the arrangement is connected then the check must pass
        if (connected) return;
        // otherwise, we add it to the appropriate list
        var arrangements = inductiveChecks.GetOrAdd(node, new List<IReadOnlyList<bool>>());
        arrangements.Add(arrangement);
      });
    return inductiveChecks;
  }

  /// <summary>
  /// Return a dictionary from nodes to witness times, such that the returned witness times
  /// ensure that all the given arrangements are blocked, or an empty dictionary if no such witness times exist.
  /// </summary>
  /// <param name="initialChecks">Nodes where the invariant failed to hold on the initial value.</param>
  /// <param name="inductiveChecks">Node arrangements where the inductive check failed with the before invariant.</param>
  /// <returns></returns>
  private Dictionary<TNode, BigInteger> InferTimesFromChecks(IEnumerable<(TNode, bool)> initialChecks,
    IReadOnlyDictionary<TNode, List<IReadOnlyList<bool>>> inductiveChecks)
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
      initialChecks.Select<(TNode, bool), Zen<bool>>(tuple =>
        tuple.Item2 ? times[tuple.Item1] == BigInteger.Zero : times[tuple.Item1] > BigInteger.Zero));

    // for each failed inductive check, we add the following bounds:
    // (1) if the before check failed for node n and b anc, add bounds for all b m in anc
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 >= t_n
    foreach (var (node, arrangements) in inductiveChecks)
    {
      bounds = (from arrangement in arrangements select BoundArrangement(node, times, arrangement))
        .Aggregate(bounds, (current, beforeBounds) => current.Union(beforeBounds));
    }

    // (2) if the after check failed for node n and b anc, add bounds for all predecessors m of n
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 < t_n,
    //     or n converges before all nodes u_j in anc (t_n - 1 < t_j), or after all nodes u_j not in anc (t_n - 1 >= t_j)
    foreach (var (node, arrangements) in inductiveChecks)
    {
      bounds = arrangements.Where(a => !a[^1])
        .Select(arrangement => TimeInterval(Digraph[node], times[node] - BigInteger.One, times, arrangement))
        .Aggregate(bounds, (current, nextBound) => current.Add(nextBound));
    }

    // print the computed bounds
    if (PrintBounds)
      foreach (var b in bounds)
        Console.WriteLine(b);

    var model = Zen.And(bounds).Solve();
    if (model.IsSatisfiable())
      return new Dictionary<TNode, BigInteger>(times.Select(pair =>
        new KeyValuePair<TNode, BigInteger>(pair.Key, model.Get(pair.Value))));
    return new Dictionary<TNode, BigInteger>();
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
  private static Zen<bool> TimeInterval(IEnumerable<TNode> predecessors, Zen<BigInteger> time,
    IReadOnlyDictionary<TNode, Zen<BigInteger>> times, IEnumerable<bool> arrangement)
  {
    var neighborBounds = predecessors.Zip(arrangement)
      // skip cases where the symbolic time is for the given predecessor,
      // but would be set to be less than itself
      // (arrangement is false and the symbolic time is equal to the predecessor's time)
      // .Where(j => arrangement[j].Value || !time.Equals(times[j]))
      .Select(t => t.Second ? time >= times[t.First] : time < times[t.First]).ToArray();
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
  /// <returns>An enumerable of constraints.</returns>
  private IEnumerable<Zen<bool>> BoundArrangement(TNode node,
    IReadOnlyDictionary<TNode, Zen<BigInteger>> times, IReadOnlyList<bool> arrangement)
  {
    // the instantiated bounds are 0 and all neighbors that have already converged (the arrangement is false at the neighbor)
    var lowerBounds = Enumerable.Repeat(Zen.Constant(BigInteger.Zero), 1)
      .Concat(Digraph[node]
        .Where((_, j) => !arrangement[j])
        .Select(neighbor => times[neighbor]));
    // for each lower bound, add a disjunction that rules out the case
    return from lowerBound in lowerBounds
      select Zen.Or(
        arrangement[^1] ? lowerBound + BigInteger.One >= times[node] : lowerBound + BigInteger.One < times[node],
        TimeInterval(Digraph[node], lowerBound, times, arrangement));
  }

  /// <summary>
  /// Log the time taken (in milliseconds) by the given function to complete.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="times">The dictionary in which the time taken should be stored.</param>
  /// <param name="f">The function to run.</param>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TResult">The return type of the function.</typeparam>
  /// <returns>The function's return value.</returns>
  private TResult LogFunctionTime<TKey, TResult>(TKey key, ConcurrentDictionary<TKey, long> times, Func<TResult> f)
    where TKey : notnull
  {
    // if we're not logging, return immediately
    if (!LogTimes) return f();
    var result = Time(f, out var timeTaken);
    var old = times.GetOrAdd(key, 0);
    // if the value is already present, increase it
    times[key] = old + timeTaken;
    return result;
  }

  private static TResult Time<TResult>(Func<TResult> f, out long timeTaken)
  {
    var timer = Stopwatch.StartNew();
    var result = f();
    timeTaken = timer.ElapsedMilliseconds;
    return result;
  }

  /// <summary>
  /// Infer suitable annotations for a Timepiece <c>AnnotatedNetwork{T,TS}</c> instance.
  /// The given inference strategy determines the form of inference used.
  /// </summary>
  /// <param name="strategy">the <c>InferenceStrategy</c> inference strategy: see <see cref="InferenceStrategy"/></param>
  /// <exception cref="ArgumentOutOfRangeException">if an invalid inference strategy is given</exception>
  /// <returns>a dictionary from nodes to temporal predicates</returns>
  public Dictionary<TNode, Func<Zen<TRoute>, Zen<BigInteger>, Zen<bool>>> InferAnnotationsWithStats(
    InferenceStrategy strategy)
  {
    LogTimes = true;
    try
    {
      Console.WriteLine("Inferring witness times...");
      var witnessTimes = Time(() => InferTimes(strategy), out var timeTaken);
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

  private static void FiveNumberSummary(IDictionary<TNode, int> d)
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

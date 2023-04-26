using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
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
    Dictionary<string, Func<Zen<T>, Zen<bool>>> afterInvariants)
  {
    Topology = topology;
    TransferFunction = transferFunction;
    MergeFunction = mergeFunction;
    InitialValues = initialValues;
    BeforeInvariants = beforeInvariants;
    AfterInvariants = afterInvariants;
  }

  public bool Verbose { get; set; } = false;

  public BigInteger? MaxTime { get; set; }
  private Topology Topology { get; }
  private Dictionary<(string, string), Func<Zen<T>, Zen<T>>> TransferFunction { get; }
  private Func<Zen<T>, Zen<T>, Zen<T>> MergeFunction { get; }
  private Dictionary<string, Zen<T>> InitialValues { get; }
  private Dictionary<string, Func<Zen<T>, Zen<bool>>> BeforeInvariants { get; }
  private Dictionary<string, Func<Zen<T>, Zen<bool>>> AfterInvariants { get; }

  /// <summary>
  ///   Return a string describing a failed check.
  ///   If b is not null, specify the b that caused the failure (inductive check);
  ///   otherwise, assume the failure is due to the initial check.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="invariantDescriptor">A descriptor of the invariant, e.g. "before" or "after".</param>
  /// <param name="b">An array of node names of the node's b, or null.</param>
  /// <returns>A string describing a failed check.</returns>
  private string ReportFailure(string node, string invariantDescriptor, BitArray? b)
  {
    if (b is not null)
    {
      var bString = new StringBuilder();
      foreach (var i in Enumerable.Range(0, Topology[node].Count))
      {
        if (bString.Length > 0) bString.Append(", ");
        // specify whether the neighbor was before or after
        bString.Append(b[i] ? "before " : "after ");
        bString.Append(Topology[node][i]);
      }

      return $"Arrangement [{bString}] does NOT imply node {node}'s {invariantDescriptor} invariant.";
    }

    return $"Node {node}'s {invariantDescriptor} invariant does not hold for its initial route.";
  }

  /// <summary>
  ///   Check that a node's initial route satisfies the given invariant.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="invariant"></param>
  /// <returns>True if the invariant does *not* hold for the initial route, and false otherwise.</returns>
  private bool CheckInitial(string node, Func<Zen<T>, Zen<bool>> invariant)
  {
    var query = Zen.Not(invariant(InitialValues[node]));
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
  /// <returns>True if the invariant is *not* always satisfied by the bs, and false otherwise.</returns>
  private List<bool>? CheckInductive(string node, Func<Zen<T>, Zen<bool>> invariant,
    IReadOnlyList<Zen<bool>> b, IReadOnlyDictionary<string, Zen<T>> routes,
    IEnumerable<Zen<bool>>? blockingClauses = null)
  {
    var newNodeRoute = UpdateNodeRoute(node, routes);

    // check predecessor invariants according to whether or not the predecessor was given in b
    // we check the before invariant of a predecessor when b[i] is true, and the after invariant when b[i] is false
    var assume = Topology[node]
      .Select((predecessor, i) =>
        Zen.If(b[i], BeforeInvariants[predecessor](routes[predecessor]),
          AfterInvariants[predecessor](routes[predecessor])));
    var check = Zen.Implies(Zen.And(assume.ToArray()), invariant(newNodeRoute));

    var query = blockingClauses is null ? Zen.Not(check) : Zen.And(Zen.And(blockingClauses), Zen.Not(check));
    var model = query.Solve();

    return model.IsSatisfiable() ? b.Select(bi => model.Get(bi)).ToList() : null;
  }

  /// <summary>
  ///   Return a route corresponding to the application of one step of the network semantics:
  ///   starting from the initial route at a node, merge in each transferred route from the node's neighbor.
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
  ///   Infer times for each node, such that a network annotated with these times (of the form "before until^{t} after")
  ///   should pass all the modular checks.
  ///   Explicitly enumerates the arrangements of before/after conditions of neighbors' routes.
  /// </summary>
  /// <returns>A dictionary mapping nodes to witness times.</returns>
  private Dictionary<string, BigInteger> InferTimesExplicit()
  {
    var afterInitialChecks = new ConcurrentBag<string>();
    var beforeInitialChecks = new ConcurrentBag<string>();
    Topology.Nodes.AsParallel().ForAll(node =>
    {
      if (CheckInitial(node, BeforeInvariants[node]))
      {
        Console.WriteLine(ReportFailure(node, "before", null));
        beforeInitialChecks.Add(node);
      }

      if (CheckInitial(node, AfterInvariants[node]))
      {
        Console.WriteLine(ReportFailure(node, "after", null));
        afterInitialChecks.Add(node);
      }
    });
    // for each node, for each subset of its predecessors, run CheckInductive in parallel
    // construct a dictionary of the results of which b fail to imply the two invariants
    var beforeInductiveChecks = new ConcurrentDictionary<string, List<BitArray>>();
    var afterInductiveChecks = new ConcurrentDictionary<string, List<BitArray>>();
    var nodeAndArrangements = Topology.Nodes
      .SelectMany(n => PowerSet.BitPSet(Topology[n].Count), (n, b) => (n, b));
    // TODO: if we have check failures when predecessor u is both in b and not in b,
    // TODO: then we should exclude it from the generated bounds (since its value won't matter)
    nodeAndArrangements.AsParallel()
      .ForAll(tuple =>
      {
        var n = tuple.n;
        var b = tuple.b.Cast<bool>().Select(Zen.Constant).ToList();
        var routes = new Dictionary<string, Zen<T>>();
        foreach (var predecessor in Topology[n]) routes[predecessor] = Zen.Symbolic<T>();
        if (CheckInductive(n, BeforeInvariants[n], b, routes) is not null)
        {
          Console.WriteLine(ReportFailure(n, "before", tuple.b));
          var ancestors = beforeInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
        }

        if (CheckInductive(n, AfterInvariants[n], b, routes) is not null)
        {
          Console.WriteLine(ReportFailure(n, "after", tuple.b));
          var ancestors = afterInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
        }
      });

    // construct a set of bounds to check
    var times = Topology.MapNodes(node => Zen.Symbolic<BigInteger>($"{node}-time"));
    var bounds = TimeBounds(times, beforeInitialChecks, afterInitialChecks, beforeInductiveChecks,
      afterInductiveChecks);

    var model = bounds.Solve();
    if (model.IsSatisfiable())
      return new Dictionary<string, BigInteger>(times.Select(pair =>
        new KeyValuePair<string, BigInteger>(pair.Key, model.Get(pair.Value))));

    return new Dictionary<string, BigInteger>();
  }

  /// <summary>
  ///   Infer times for each node, such that a network annotated with these times (of the form "before until^{t} after")
  ///   should pass all the modular checks.
  ///   Symbolically enumerates the inductive condition arrangements of before/after conditions of neighbors' routes
  ///   by asking the solver to find failing arrangements.
  /// </summary>
  /// <returns>A dictionary mapping nodes to witness times.</returns>
  private Dictionary<string, BigInteger> InferTimesSymbolic()
  {
    var afterInitialChecks = new ConcurrentBag<string>();
    var beforeInitialChecks = new ConcurrentBag<string>();
    Topology.Nodes.AsParallel().ForAll(node =>
    {
      if (CheckInitial(node, BeforeInvariants[node]))
      {
        Console.WriteLine(ReportFailure(node, "before", null));
        beforeInitialChecks.Add(node);
      }

      if (CheckInitial(node, AfterInvariants[node]))
      {
        Console.WriteLine(ReportFailure(node, "after", null));
        afterInitialChecks.Add(node);
      }
    });
    // for each node, for each subset of its predecessors, run CheckInductive in parallel
    // construct a dictionary of the results of which b fail to imply the two invariants
    var beforeInductiveChecks = new ConcurrentDictionary<string, List<BitArray>>();
    var afterInductiveChecks = new ConcurrentDictionary<string, List<BitArray>>();
    var nodeAndArrangements = Topology.Nodes
      .Select(n =>
      {
        // generate an array of symbolic booleans of length equal to the node's predecessors + 1
        var neighbors = Topology[n].Count;
        return (n, b: Enumerable.Range(0, neighbors + 1).Select(i => Zen.Symbolic<bool>($"b{i}")));
      });
    nodeAndArrangements.AsParallel()
      .ForAll(tuple =>
      {
        var n = tuple.n;
        var b = tuple.b.ToList();
        var blockingClauses = new List<Zen<bool>>();
        var routes = new Dictionary<string, Zen<T>>();
        foreach (var neighbor in Topology[n]) routes[neighbor] = Zen.Symbolic<T>();
        var bSol = CheckInductive(n, r => Zen.If(b[^1], BeforeInvariants[n](r), AfterInvariants[n](r)), b, routes);
        while (bSol is not null)
        {
          // get the model and block it
          var bArr = new BitArray(bSol.ToArray());
          Console.WriteLine(ReportFailure(n, bSol[^1] ? "before" : "after", bArr));
          // construct a blocking clause: a negation of the case where all the B variables are as found by the solver
          var blockBs = bSol.Select((bb, i) => bb ? Zen.Not(b[i]) : b[i]);
          var blockingClause = Zen.Or(blockBs);
          blockingClauses.Add(blockingClause);
          // save this case as one to generate constraints for
          var arr = (bSol[^1] ? beforeInductiveChecks : afterInductiveChecks).GetOrAdd(n, new List<BitArray>());
          arr.Add(bArr);
          bSol =
            CheckInductive(n, r => Zen.If(b[^1], BeforeInvariants[n](r), AfterInvariants[n](r)), b, routes,
              blockingClauses: blockingClauses);
        }
      });

    // construct a set of bounds to check
    var times = Topology.MapNodes(node => Zen.Symbolic<BigInteger>($"{node}-time"));
    var bounds = TimeBounds(times, beforeInitialChecks, afterInitialChecks, beforeInductiveChecks,
      afterInductiveChecks);

    var model = bounds.Solve();
    if (model.IsSatisfiable())
      return new Dictionary<string, BigInteger>(times.Select(pair =>
        new KeyValuePair<string, BigInteger>(pair.Key, model.Get(pair.Value))));

    return new Dictionary<string, BigInteger>();
  }

  private Zen<bool> TimeBounds(IReadOnlyDictionary<string, Zen<BigInteger>> times,
    ConcurrentBag<string> beforeInitialChecks,
    ConcurrentBag<string> afterInitialChecks, ConcurrentDictionary<string, List<BitArray>> beforeInductiveChecks,
    ConcurrentDictionary<string, List<BitArray>> afterInductiveChecks)
  {
    // add initial check bounds
    var bounds =
      beforeInitialChecks.Select<string, Zen<bool>>(node => times[node] == BigInteger.Zero)
        .Concat(afterInitialChecks.Select<string, Zen<bool>>(node => times[node] > BigInteger.Zero)).ToList();
    // if a maximum time is given, also require that no witness time is greater than the maximum
    if (MaxTime is not null) bounds.AddRange(times.Select(pair => pair.Value <= MaxTime));
    // for each failed inductive check, we add the following bounds:
    // (1) if the before check failed for node n and b anc, add bounds for all b m in anc
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 >= t_n
    foreach (var (node, arrangements) in beforeInductiveChecks)
    foreach (var arrangement in arrangements)
    {
      var zeroBeforeBound = Zen.Or(BigInteger.One >= times[node],
        Zen.Not(NextToConverge(Topology[node], BigInteger.Zero, times, arrangement)));
      bounds.Add(zeroBeforeBound);
      var neighbors = Topology[node].Where((_, i) => !arrangement[i]);
      var beforeBounds = from neighbor in neighbors
        select Zen.Or(times[neighbor] + BigInteger.One >= times[node],
          Zen.Not(NextToConverge(Topology[node], times[neighbor], times, arrangement)));
      bounds.AddRange(beforeBounds);
    }

    // (2) if the after check failed for node n and b anc, add bounds for all predecessors m of n
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 < t_n,
    //     or n converges before all nodes u_j in anc (t_n - 1 < t_j), or after all nodes u_j not in anc (t_n - 1 >= t_j)
    foreach (var (node, arrangements) in afterInductiveChecks)
    foreach (var arrangement in arrangements)
    {
      var zeroAfterBound = Zen.Or(BigInteger.One < times[node],
        Zen.Not(NextToConverge(Topology[node], BigInteger.Zero, times, arrangement)));
      bounds.Add(zeroAfterBound);
      var neighbors = Topology[node].Where((_, i) => !arrangement[i]);
      var afterBounds = from neighbor in neighbors
        select Zen.Or(times[neighbor] + BigInteger.One < times[node],
          Zen.Not(NextToConverge(Topology[node], times[neighbor], times, arrangement)));
      bounds.AddRange(afterBounds);
      var nextBound = Zen.Not(NextToConverge(Topology[node], times[node] - BigInteger.One, times, arrangement));
      bounds.Add(nextBound);
    }

    if (Verbose)
      // list the computed bounds
      foreach (var b in bounds)
        Console.WriteLine(b);

    // return a boolean formula over the times
    var nonNegativeTimes = Zen.And(times.Select(t => t.Value >= BigInteger.Zero));
    return Zen.And(Zen.And(bounds), nonNegativeTimes);
  }

  /// <summary>
  ///   Generate a conjunction of constraints on the given time: for the given predecessors and time,
  ///   the constraints require that each of the predecessor's times is at most the given time
  ///   if the predecessor is also an ancestor, and otherwise strictly greater than the given time.
  /// </summary>
  /// <param name="predecessors"></param>
  /// <param name="time"></param>
  /// <param name="times"></param>
  /// <param name="b"></param>
  /// <returns></returns>
  private static Zen<bool> NextToConverge(IEnumerable<string> predecessors, Zen<BigInteger> time,
    IReadOnlyDictionary<string, Zen<BigInteger>> times, BitArray b)
  {
    return Zen.And(predecessors.Select((j, i) => b[i] ? time < times[j] : time >= times[j]));
  }

  private (long, Dictionary<string, BigInteger>) InferTimesTimed(Func<Dictionary<string, BigInteger>> inferFunc)
  {
    var timer = Stopwatch.StartNew();
    var times = inferFunc();
    return (timer.ElapsedMilliseconds, times);
  }

  /// <summary>
  ///   Convert the inference problem into a Timepiece network instance.
  /// </summary>
  /// <typeparam name="TS"></typeparam>
  /// <returns></returns>
  public Network<T, TS> ToNetwork<TS>(InferenceStrategy strategy)
  {
    long timeTaken;
    Dictionary<string, BigInteger> times;
    switch (strategy)
    {
      case InferenceStrategy.ExplicitEnumeration:
        (timeTaken, times) = InferTimesTimed(InferTimesExplicit);
        Console.WriteLine($"Inference took {timeTaken}ms!");
        break;
      case InferenceStrategy.SymbolicEnumeration:
        (timeTaken, times) = InferTimesTimed(InferTimesSymbolic);
        Console.WriteLine($"Inference took {timeTaken}ms!");
        break;
      case InferenceStrategy.Compare:
        var (explicitTimeTaken, explicitTimes) = InferTimesTimed(InferTimesExplicit);
        var (symbolicTimeTaken, symbolicTimes) = InferTimesTimed(InferTimesSymbolic);
        Console.WriteLine($"Explicit inference took {explicitTimeTaken}ms!");
        Console.WriteLine($"Symbolic inference took {symbolicTimeTaken}ms!");
        var consistentTimes = true;
        foreach (var (node, explicitTime) in explicitTimes)
        {
          var symbolicTime = symbolicTimes[node];
          if (symbolicTime == explicitTime) continue;
          Console.WriteLine(
            $"Node {node}: explicit time {explicitTime} is not the same as symbolic time {symbolicTime}");
          consistentTimes = false;
          break;
        }

        Console.WriteLine(consistentTimes ? "Inference was consistent!" : "Inference was inconsistent!");
        times = symbolicTimes;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
    }

    if (times.Count > 0)
    {
      Console.WriteLine("Success, inferred the following times:");
      foreach (var (node, time) in times) Console.WriteLine($"{node}: {time}");
    }
    else
    {
      throw new ArgumentException("Failed to infer times!");
    }

    var annotations = Topology.MapNodes(n => Lang.Until(times[n], BeforeInvariants[n], AfterInvariants[n]));
    return new Network<T, TS>(Topology, TransferFunction, MergeFunction, InitialValues, annotations, annotations,
      AfterInvariants, new SymbolicValue<TS>[] { });
  }
}

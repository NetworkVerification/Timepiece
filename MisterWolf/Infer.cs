using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Timepiece;
using Timepiece.Networks;
using ZenLib;
using Array = System.Array;

namespace MisterWolf;

public class Infer<T> : Network<T, Unit>
{
  public Infer(Topology topology,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<string, Zen<T>> initialValues,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> afterInvariants) : base(topology, transferFunction, mergeFunction,
    initialValues, Array.Empty<SymbolicValue<Unit>>())
  {
    BeforeInvariants = beforeInvariants;
    AfterInvariants = afterInvariants;
  }

  public Infer(Network<T, Unit> net, IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> afterInvariants) : this(net.Topology, net.TransferFunction,
    net.MergeFunction, net.InitialValues, beforeInvariants, afterInvariants) {}

  /// <summary>
  /// If true, print the generated bounds to standard output.
  /// </summary>
  public bool PrintBounds { get; set; } = false;

  /// <summary>
  /// If true, report all failures to standard output.
  /// </summary>
  public bool ReportFailures { get; set; } = false;

  /// <summary>
  /// If true, report all times inferred to standard output.
  /// </summary>
  public bool PrintTimes { get; set; } = false;

  public BigInteger? MaxTime { get; set; }
  protected IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> BeforeInvariants { get; }
  protected IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> AfterInvariants { get; }

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

  private void PrintFailure(string node, string invariantDescriptor, BitArray? b) {
    if (ReportFailures)
      Console.WriteLine(ReportFailure(node, invariantDescriptor, b));
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
  ///   Infer times for each node, such that a network annotated with these times (of the form "before until^{t} after")
  ///   should pass all the modular checks.
  ///   Explicitly enumerates the arrangements of before/after conditions of neighbors' routes.
  /// </summary>
  /// <returns>A dictionary mapping nodes to witness times.</returns>
  public Dictionary<string, BigInteger> InferTimesExplicit()
  {
    var afterInitialChecks = new ConcurrentBag<string>();
    var beforeInitialChecks = new ConcurrentBag<string>();
    Topology.Nodes.AsParallel().ForAll(node =>
    {
      if (CheckInitial(node, BeforeInvariants[node]))
      {
        PrintFailure(node, "before", null);
        beforeInitialChecks.Add(node);
      }

      if (CheckInitial(node, AfterInvariants[node]))
      {
        PrintFailure(node, "after", null);
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
          PrintFailure(n, "before", tuple.b);
          var ancestors = beforeInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
        }

        if (CheckInductive(n, AfterInvariants[n], b, routes) is not null)
        {
          PrintFailure(n, "after", tuple.b);
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
  public Dictionary<string, BigInteger> InferTimesSymbolic()
  {
    var afterInitialChecks = new ConcurrentBag<string>();
    var beforeInitialChecks = new ConcurrentBag<string>();
    Topology.Nodes.AsParallel().ForAll(node =>
    {
      if (CheckInitial(node, BeforeInvariants[node]))
      {
        PrintFailure(node, "before", null);
        beforeInitialChecks.Add(node);
      }

      if (CheckInitial(node, AfterInvariants[node]))
      {
        PrintFailure(node, "after", null);
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
          PrintFailure(n, bSol[^1] ? "before" : "after", bArr);
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
    // enforce that all times must be non-negative
    var bounds = times.Select(t => t.Value >= BigInteger.Zero).ToList();
    // if a maximum time is given, also require that no witness time is greater than the maximum
    if (MaxTime is not null) bounds.AddRange(times.Select(pair => pair.Value <= MaxTime));

    // add initial check bounds
    bounds.AddRange(
      beforeInitialChecks.Select<string, Zen<bool>>(node => times[node] == BigInteger.Zero)
        .Concat(afterInitialChecks.Select<string, Zen<bool>>(node => times[node] > BigInteger.Zero)));

    var simplifiedBeforeInductiveChecks = beforeInductiveChecks.Select(p =>
      new KeyValuePair<string, IEnumerable<bool?[]>>(p.Key, PrimeArrangements.SimplifyArrangements(p.Value)));
    var simplifiedAfterInductiveChecks = afterInductiveChecks.Select(p =>
      new KeyValuePair<string, IEnumerable<bool?[]>>(p.Key, PrimeArrangements.SimplifyArrangements(p.Value)));
    // for each failed inductive check, we add the following bounds:
    // (1) if the before check failed for node n and b anc, add bounds for all b m in anc
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 >= t_n
    foreach (var (node, arrangements) in simplifiedBeforeInductiveChecks)
    foreach (IEnumerable<Zen<bool>>? beforeBounds in from arrangement in arrangements
             select BoundArrangement(node, times, arrangement))
    {
      bounds.AddRange(beforeBounds);
    }

    // (2) if the after check failed for node n and b anc, add bounds for all predecessors m of n
    //     where m converges before all nodes u_j in anc (t_m < t_j), or after all nodes u_j not in anc (t_m >= t_j),
    //     or t_m + 1 < t_n,
    //     or n converges before all nodes u_j in anc (t_n - 1 < t_j), or after all nodes u_j not in anc (t_n - 1 >= t_j)
    foreach (var (node, arrangements) in simplifiedAfterInductiveChecks)
    foreach (var arrangement in arrangements)
    {
      bounds.AddRange(BoundArrangement(node, times, arrangement));
      // var nextBound = Zen.Not(TimeInterval(Topology[node], times[node] - BigInteger.One, times, arrangement));
      var (earlierNeighbors, laterNeighbors) = PartitionNeighborsByArrangement(node, arrangement);
      var nextBound = Zen.Not(TimeInterval(earlierNeighbors, laterNeighbors, times[node] - BigInteger.One, times));
      bounds.Add(nextBound);
    }

    if (PrintBounds)
      // list the computed bounds
      foreach (var b in bounds)
        Console.WriteLine(b);

    return Zen.And(bounds);
  }

  /// <summary>
  ///   Generate a conjunction of constraints on the given time: for the given predecessors and time,
  ///   the constraints require that each of the predecessor's times is at most the given time
  ///   if the predecessor is set in the arrangement, and otherwise strictly greater than the given time.
  /// </summary>
  /// <param name="predecessors">The predecessor nodes.</param>
  /// <param name="time">The symbolic time.</param>
  /// <param name="times">The witness times of the predecessors.</param>
  /// <param name="arrangement">
  /// The arrangement of the predecessors, such that if arrangement[i] is true for predecessor i,
  /// then the given symbolic time is less than predecessor i's witness time, and otherwise greater than or equal
  /// (when arrangement[i] is false).</param>
  /// <returns>A conjunction over comparisons between time and the witness times.</returns>
  private static Zen<bool> TimeInterval(IEnumerable<string> predecessors, Zen<BigInteger> time,
    IReadOnlyDictionary<string, Zen<BigInteger>> times, BitArray arrangement)
  {
    return Zen.And(predecessors.Select((j, i) => arrangement[i] ? time < times[j] : time >= times[j]));
  }

  private static Zen<bool> TimeInterval(IEnumerable<string> earlierNeighbors,
    IEnumerable<string> laterNeighbors,
    Zen<BigInteger> time,
    IReadOnlyDictionary<string, Zen<BigInteger>> times)
  {
    var neighborBounds = earlierNeighbors.Select(en => time < times[en])
      .Concat(laterNeighbors.Select(ln => time >= times[ln])).ToArray();
    return neighborBounds.Length > 0 ? Zen.And(neighborBounds) : true;
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
  /// <param name="arrangement">A bitarray representing the neighbors and node, in order (neighbors then node).</param>
  /// <returns>An enumerable of constraints.</returns>
  private IEnumerable<Zen<bool>> BoundArrangement(string node,
    IReadOnlyDictionary<string, Zen<BigInteger>> times, BitArray arrangement)
  {
    // the instantiated bounds are 0 and all neighbors that have already converged (the arrangement is false at the neighbor)
    var lowerBounds = Enumerable.Repeat(Zen.Constant(BigInteger.Zero), 1)
      .Concat(from neighbor in Topology[node].Where((_, i) => !arrangement[i]) // indexed where has no query syntax form
        select times[neighbor]);
    // for each lower bound, add a disjunction that rules out the case
    return from lowerBound in lowerBounds
      select Zen.Or(
        arrangement[^1] ? lowerBound + BigInteger.One >= times[node] : lowerBound + BigInteger.One < times[node],
        Zen.Not(TimeInterval(Topology[node], lowerBound, times, arrangement)));
  }

  private (List<string>, List<string>) PartitionNeighborsByArrangement(string node, IReadOnlyList<bool?> arrangement)
  {
    var earlierNeighbors = new List<string>();
    var laterNeighbors = new List<string>();
    for (var i = 0; i < Topology[node].Count; i++)
    {
      if (arrangement[i] is null) continue;
      if ((bool) arrangement[i]!)
      {
        earlierNeighbors.Add(Topology[node][i]);
      }
      else
      {
        laterNeighbors.Add(Topology[node][i]);
      }
    }

    return (earlierNeighbors, laterNeighbors);
  }

  private IEnumerable<Zen<bool>> BoundArrangement(string node, IReadOnlyDictionary<string, Zen<BigInteger>> times,
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
        Zen.Not(TimeInterval(earlierNeighbors, laterNeighbors, times[node], times)));
  }

  public static (long, Dictionary<string, BigInteger>) InferTimesTimed(Func<Dictionary<string, BigInteger>> inferFunc)
  {
    var timer = Stopwatch.StartNew();
    var times = inferFunc();
    return (timer.ElapsedMilliseconds, times);
  }

  /// <summary>
  /// Infer suitable annotations for a Timepiece <c>AnnotatedNetwork{T,TS}</c> instance.
  /// The given inference strategy determines the form of inference used.
  /// </summary>
  /// <param name="strategy">the <c>InferenceStrategy</c> inference strategy: see <see cref="InferenceStrategy"/></param>
  /// <returns>a dictionary from nodes to temporal predicates</returns>
  public Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> InferAnnotations(InferenceStrategy strategy)
  {
    Console.WriteLine("Inferring witness times...");
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
      if (PrintTimes)
      {
        Console.WriteLine("Success, inferred the following times:");
        foreach (var (node, time) in times) Console.WriteLine($"{node}: {time}");
      }
    }
    else
    {
      throw new ArgumentException("Failed to infer times!");
    }

    return Topology.MapNodes(n => Lang.Until(times[n], BeforeInvariants[n], AfterInvariants[n]));
  }
}

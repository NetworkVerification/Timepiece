using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Timepiece;
using Timepiece.Networks;
using ZenLib;

namespace MisterWolf;

public class Infer<T, TV, TS> : Network<T, TV, TS> where TV : notnull
{
  public Infer(Topology<TV> topology,
    Dictionary<(TV, TV), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<TV, Zen<T>> initialValues,
    IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> afterInvariants,
    SymbolicValue<TS>[] symbolics) : base(topology, transferFunction, mergeFunction,
    initialValues, symbolics)
  {
    BeforeInvariants = beforeInvariants;
    AfterInvariants = afterInvariants;
    NumInductiveFailures = topology.MapNodes(_ => 0);
  }

  public Infer(Network<T, TV, TS> net, IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> afterInvariants) : this(net.Topology, net.TransferFunction,
    net.MergeFunction, net.InitialValues, beforeInvariants, afterInvariants, net.Symbolics)
  {
  }

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
  protected IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> BeforeInvariants { get; }
  protected IReadOnlyDictionary<TV, Func<Zen<T>, Zen<bool>>> AfterInvariants { get; }

  /// <summary>
  ///   Return a string describing a failed check.
  ///   If b is not null, specify the b that caused the failure (inductive check);
  ///   otherwise, assume the failure is due to the initial check.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <param name="invariantDescriptor">A descriptor of the invariant, e.g. "before" or "after".</param>
  /// <param name="b">An array of node names of the node's b, or null.</param>
  /// <returns>A string describing a failed check.</returns>
  private string ReportFailure(TV node, string invariantDescriptor, BitArray? b)
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

  private void PrintFailure(TV node, string invariantDescriptor, BitArray? b)
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
  /// <returns>True if the invariant is *not* always satisfied by the bs, and false otherwise.</returns>
  private List<bool>? CheckInductive(TV node, Func<Zen<T>, Zen<bool>> invariant,
    IReadOnlyList<Zen<bool>> b, IReadOnlyDictionary<TV, Zen<T>> routes,
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

    var query = blockingClauses is null
      ? Zen.And(GetSymbolicConstraints(), Zen.Not(check))
      : Zen.And(GetSymbolicConstraints(), Zen.And(blockingClauses), Zen.Not(check));
    var model = query.Solve();

    return model.IsSatisfiable() ? b.Select(bi => model.Get(bi)).ToList() : null;
  }

  public Dictionary<TV, BigInteger> InferTimes(InferenceStrategy strategy)
  {
    var (beforeInitialChecks, afterInitialChecks) = FailingInitialChecks();
    // construct dictionaries listing which arrangements fail to imply the before and after invariants
    IReadOnlyDictionary<TV, List<BitArray>> beforeInductiveChecks;
    IReadOnlyDictionary<TV, List<BitArray>> afterInductiveChecks;
    switch (strategy)
    {
      case InferenceStrategy.ExplicitEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) = EnumerateArrangementsExplicit();
        break;
      case InferenceStrategy.SymbolicEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) = EnumerateArrangementsSymbolic();
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
    }

    return InferTimesFromChecks(beforeInitialChecks, afterInitialChecks, beforeInductiveChecks, afterInductiveChecks);
  }

  public Dictionary<TV, BigInteger> InferTimesWith<TAcc1, TAcc2>(InferenceStrategy strategy,
    TAcc1 beforeInitialCollector,
    TAcc1 afterInitialCollector, TAcc2 beforeInductiveCollector, TAcc2 afterInductiveCollector,
    Action<TV, TAcc1, Action> initialF, Action<TV, BitArray, TAcc2, Action> inductiveF)
  {
    var (beforeInitialChecks, afterInitialChecks) =
      FailingInitialChecksWith(beforeInitialCollector, afterInitialCollector, initialF);
    // for each node, for each subset of its predecessors, run CheckInductive in parallel
    // construct a dictionary of the results of which b fail to imply the two invariants
    IReadOnlyDictionary<TV, List<BitArray>> beforeInductiveChecks;
    IReadOnlyDictionary<TV, List<BitArray>> afterInductiveChecks;
    switch (strategy)
    {
      case InferenceStrategy.ExplicitEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) =
          EnumerateArrangementsExplicitWith(beforeInductiveCollector, afterInductiveCollector, inductiveF);
        break;
      case InferenceStrategy.SymbolicEnumeration:
        (beforeInductiveChecks, afterInductiveChecks) =
          EnumerateArrangementsSymbolicWith(beforeInductiveCollector,
            (n, acc, g) => inductiveF(n, new BitArray(0), acc, g));
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
    }

    return InferTimesFromChecks(beforeInitialChecks, afterInitialChecks, beforeInductiveChecks, afterInductiveChecks);
  }

  private (IReadOnlyCollection<TV>, IReadOnlyCollection<TV>) FailingInitialChecks()
  {
    var afterInitialChecks = new ConcurrentBag<TV>();
    var beforeInitialChecks = new ConcurrentBag<TV>();
    Topology.Nodes.AsParallel().ForAll(node =>
    {
      if (CheckInitial(node, BeforeInvariants[node])) beforeInitialChecks.Add(node);
      if (CheckInitial(node, AfterInvariants[node])) afterInitialChecks.Add(node);
    });
    return (beforeInitialChecks, afterInitialChecks);
  }

  /// <summary>
  /// Check all nodes' initial values against the invariants and return which nodes' invariants failed.
  /// </summary>
  /// <returns>Two collections collecting all nodes that failed the before invariant (the first collection),
  /// or the second invariant (the second collection).</returns>
  private (IReadOnlyCollection<TV>, IReadOnlyCollection<TV>) FailingInitialChecksWith<TAcc>(TAcc beforeCollector,
    TAcc afterCollector, Action<TV, TAcc, Action> f)
  {
    var afterInitialChecks = new ConcurrentBag<TV>();
    var beforeInitialChecks = new ConcurrentBag<TV>();
    Topology.Nodes.AsParallel().ForAll(node =>
    {
      f(node, beforeCollector, () =>
      {
        if (CheckInitial(node, BeforeInvariants[node]))
        {
          PrintFailure(node, "before", null);
          beforeInitialChecks.Add(node);
        }
      });

      f(node, afterCollector, () =>
      {
        if (CheckInitial(node, AfterInvariants[node]))
        {
          PrintFailure(node, "after", null);
          afterInitialChecks.Add(node);
        }
      });
    });
    return (beforeInitialChecks, afterInitialChecks);
  }

  /// <summary>
  ///   Explicitly enumerates the arrangements of before/after conditions of neighbors' routes.
  /// </summary>
  /// <returns>Two dictionaries mapping nodes to arrangements that fail the checks.</returns>
  private (IReadOnlyDictionary<TV, List<BitArray>>, IReadOnlyDictionary<TV, List<BitArray>>)
    EnumerateArrangementsExplicit()
  {
    var processes = Environment.ProcessorCount;
    var beforeInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    var afterInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    var explicitArrangements = Topology.Nodes
      .SelectMany(n => PowerSet.BitPSet(Topology[n].Count), (n, b) => (n, b));
    explicitArrangements.AsParallel()
      .ForAll(tuple =>
      {
        var n = tuple.n;
        var b = tuple.b.Cast<bool>().Select(Zen.Constant).ToList();
        var routes = Topology[n].ToDictionary(predecessor => predecessor, _ => Zen.Symbolic<T>());

        if (CheckInductive(n, BeforeInvariants[n], b, routes) is not null)
        {
          NumInductiveFailures[n]++;
          var ancestors = beforeInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
        }

        if (CheckInductive(n, AfterInvariants[n], b, routes) is not null)
        {
          NumInductiveFailures[n]++;
          var ancestors = afterInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
        }
      });
    return (beforeInductiveChecks, afterInductiveChecks);
  }

  /// <summary>
  ///   Explicitly enumerates the arrangements of before/after conditions of neighbors' routes.
  /// </summary>
  /// <returns>Two dictionaries mapping nodes to arrangements that fail the checks.</returns>
  private (IReadOnlyDictionary<TV, List<BitArray>>, IReadOnlyDictionary<TV, List<BitArray>>)
    EnumerateArrangementsExplicitWith<TAcc>(TAcc beforeCollector, TAcc afterCollector,
      Action<TV, BitArray, TAcc, Action> f)
  {
    var processes = Environment.ProcessorCount;
    var beforeInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    var afterInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    Topology.Nodes
      // generate 2^{Topology[n].Count} arrangements for each node
      .SelectMany(n => PowerSet.BitPSet(Topology[n].Count), (n, b) => (n, b))
      .AsParallel()
      .ForAll(tuple =>
      {
        var n = tuple.n;
        var b = tuple.b.Cast<bool>().Select(Zen.Constant).ToList();
        var routes = Topology[n].ToDictionary(predecessor => predecessor, _ => Zen.Symbolic<T>());
        f(n, tuple.b, beforeCollector, () =>
        {
          if (CheckInductive(n, BeforeInvariants[n], b, routes) is null) return;
          NumInductiveFailures[n]++;
          PrintFailure(n, "before", tuple.b);
          var ancestors = beforeInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
        });

        f(n, tuple.b, afterCollector, () =>
        {
          if (CheckInductive(n, AfterInvariants[n], b, routes) is null) return;
          NumInductiveFailures[n]++;
          PrintFailure(n, "after", tuple.b);
          var ancestors = afterInductiveChecks.GetOrAdd(n, new List<BitArray>());
          ancestors.Add(tuple.b);
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
  private IEnumerable<BitArray> FindAllArrangements(TV node)
  {
    // keep track of the arrangements that fail
    var foundArrangements = new List<BitArray>();
    // generate an array of symbolic booleans of length equal to the node's predecessors + 1
    var neighbors = Topology[node].Count;
    var b = Enumerable.Range(0, neighbors + 1).Select(i => Zen.Symbolic<bool>($"b{i}")).ToList();
    var blockingClauses = new List<Zen<bool>>();
    var routes = Topology[node].ToDictionary(neighbor => neighbor, _ => Zen.Symbolic<T>());
    var bSol = CheckInductive(node, r => Zen.If(b[^1], BeforeInvariants[node](r), AfterInvariants[node](r)), b, routes);
    while (bSol is not null)
    {
      // get the model and block it
      var bArr = new BitArray(bSol.ToArray());
      PrintFailure(node, bArr[^1] ? "before" : "after", bArr);
      // construct a blocking clause: a negation of the case where all the B variables are as found by the solver
      var blockBs = bSol.Select((bb, i) => bb ? Zen.Not(b[i]) : b[i]);
      var blockingClause = Zen.Or(blockBs);
      blockingClauses.Add(blockingClause);
      // save this case as one to generate constraints for
      foundArrangements.Add(bArr);
      bSol =
        CheckInductive(node, r => Zen.If(b[^1], BeforeInvariants[node](r), AfterInvariants[node](r)), b, routes,
          blockingClauses: blockingClauses);
    }

    return foundArrangements;
  }

  /// <summary>
  ///   Symbolically enumerates the inductive condition arrangements of before/after conditions of neighbors' routes
  ///   by asking the solver to find failing arrangements.
  /// </summary>
  /// <returns>Two dictionaries mapping nodes to failing arrangements.</returns>
  private (IReadOnlyDictionary<TV, List<BitArray>>, IReadOnlyDictionary<TV, List<BitArray>>)
    EnumerateArrangementsSymbolic()
  {
    var processes = Environment.ProcessorCount;
    var beforeInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    var afterInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    Topology.Nodes
      .AsParallel()
      .ForAll(node =>
      {
        var arrangements = FindAllArrangements(node);
        foreach (var arr in arrangements)
        {
          // NOTE: could we simplify further and just use one dictionary?
          var failed = (arr[^1] ? beforeInductiveChecks : afterInductiveChecks).GetOrAdd(node, new List<BitArray>());
          failed.Add(arr);
          NumInductiveFailures[node]++;
        }
      });
    return (beforeInductiveChecks, afterInductiveChecks);
  }

  private (IReadOnlyDictionary<TV, List<BitArray>>, IReadOnlyDictionary<TV, List<BitArray>>)
    EnumerateArrangementsSymbolicWith<TAcc>(TAcc collector, Action<TV, TAcc, Action> f)
  {
    var processes = Environment.ProcessorCount;
    var beforeInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    var afterInductiveChecks = new ConcurrentDictionary<TV, List<BitArray>>(processes * 2, Topology.Nodes.Length);
    Topology.Nodes
      .AsParallel()
      .ForAll(node =>
      {
        f(node, collector, () =>
        {
          var arrangements = FindAllArrangements(node);
          foreach (var arr in arrangements)
          {
            // NOTE: could we simplify further and just use one dictionary?
            var failed = (arr[^1] ? beforeInductiveChecks : afterInductiveChecks).GetOrAdd(node, new List<BitArray>());
            failed.Add(arr);
            NumInductiveFailures[node]++;
          }
        });
      });
    return (beforeInductiveChecks, afterInductiveChecks);
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
    IEnumerable<TV> afterInitialChecks, IReadOnlyDictionary<TV, List<BitArray>> beforeInductiveChecks,
    IReadOnlyDictionary<TV, List<BitArray>> afterInductiveChecks)
  {
    // TODO: if we have check failures when predecessor u is both in b and not in b,
    // TODO: then we should exclude it from the generated bounds (since its value won't matter)
    var times = Topology.MapNodes(node => Zen.Symbolic<BigInteger>($"{node}-time"));
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
      var nextBound = TimeInterval(Topology[node], times[node] - BigInteger.One, times, arrangement);
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
    IReadOnlyDictionary<TV, Zen<BigInteger>> times, BitArray arrangement)
  {
    return Zen.Or(predecessors.Select((j, i) => arrangement[i] ? time >= times[j] : time < times[j]));
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
  /// <param name="arrangement">A bitarray representing the neighbors and node, in order (neighbors then node).</param>
  /// <param name="before">True if the bound is for the given node's before invariant, and false for its after invariant.</param>
  /// <returns>An enumerable of constraints.</returns>
  private IEnumerable<Zen<bool>> BoundArrangement(TV node,
    IReadOnlyDictionary<TV, Zen<BigInteger>> times, BitArray arrangement, bool before)
  {
    // the instantiated bounds are 0 and all neighbors that have already converged (the arrangement is false at the neighbor)
    var lowerBounds = Enumerable.Repeat(Zen.Constant(BigInteger.Zero), 1)
      .Concat(from neighbor in Topology[node].Where((_, i) => !arrangement[i]) // indexed where has no query syntax form
        select times[neighbor]);
    // for each lower bound, add a disjunction that rules out the case
    return from lowerBound in lowerBounds
      select Zen.Or(
        before ? lowerBound + BigInteger.One >= times[node] : lowerBound + BigInteger.One < times[node],
        TimeInterval(Topology[node], lowerBound, times, arrangement));
  }

  private (List<TV>, List<TV>) PartitionNeighborsByArrangement(TV node, IReadOnlyList<bool?> arrangement)
  {
    var earlierNeighbors = new List<TV>();
    var laterNeighbors = new List<TV>();
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

  public static void LogActionTime<TKey>(TKey key, IDictionary<TKey, long> times, Action inferAction)
  {
    var timer = Stopwatch.StartNew();
    inferAction();
    times.Add(key, timer.ElapsedMilliseconds);
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
    var processes = Environment.ProcessorCount;
    Console.WriteLine($"Environment.ProcessorCount: {processes}");
    var numNodes = Topology.Nodes.Length;
    var inferBeforeInitialTimes = new ConcurrentDictionary<TV, long>(processes * 2, numNodes);
    var inferAfterInitialTimes = new ConcurrentDictionary<TV, long>(processes * 2, numNodes);
    var inferBeforeInductiveTimes = new ConcurrentDictionary<(TV, BitArray), long>(processes * 2, numNodes);
    var inferAfterInductiveTimes = new ConcurrentDictionary<(TV, BitArray), long>(processes * 2, numNodes);
    try
    {
      Console.WriteLine("Inferring witness times...");
      var (timeTaken, witnessTimes) = Time(() => InferTimesWith(strategy, inferBeforeInitialTimes,
        inferAfterInitialTimes,
        inferBeforeInductiveTimes, inferAfterInductiveTimes,
        LogActionTime,
        (node, arr, times, f) => LogActionTime((node, arr), times, f)));
      Console.WriteLine($"Inference took {timeTaken}ms!");

      if (witnessTimes.Count > 0)
      {
        if (PrintTimes)
        {
          Console.WriteLine("Success, inferred the following times:");
          foreach (var (node, time) in witnessTimes) Console.WriteLine($"{node}: {time}");
        }

        return Topology.MapNodes(n => Lang.Until(witnessTimes[n], BeforeInvariants[n], AfterInvariants[n]));
      }
    }
    catch (ZenException e)
    {
      Console.WriteLine("Error, inference did not complete:");
      Console.WriteLine(e.Message);
    }
    finally
    {
      Console.WriteLine("Before initial statistics:");
      StatisticsExtensions.ReportTimes(inferBeforeInitialTimes, Statistics.Summary, null, false);
      Console.WriteLine("After initial statistics:");
      StatisticsExtensions.ReportTimes(inferAfterInitialTimes, Statistics.Summary, null, false);
      Console.WriteLine("Inductive failure statistics:");
      FiveNumberSummary(NumInductiveFailures);
      switch (strategy)
      {
        case InferenceStrategy.ExplicitEnumeration:
          Console.WriteLine("Before inductive statistics:");
          StatisticsExtensions.ReportTimes(inferBeforeInductiveTimes, Statistics.Summary, null, false);
          Console.WriteLine("After inductive statistics:");
          StatisticsExtensions.ReportTimes(inferAfterInductiveTimes, Statistics.Summary, null, false);
          break;
        case InferenceStrategy.SymbolicEnumeration:
          var inferMaxInductiveTimes = inferBeforeInductiveTimes
            .ToDictionary(p => p.Key.Item1, p => p.Value);
          Console.WriteLine("Inductive statistics:");
          StatisticsExtensions.ReportTimes(inferMaxInductiveTimes, Statistics.Summary, null, false);
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

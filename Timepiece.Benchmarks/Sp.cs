using System.Numerics;
using MisterWolf;
using Timepiece.Networks;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Benchmarks;

public class Sp<TS> : Network<Option<BgpRoute>, TS>
{
  /// <summary>
  ///   Construct a network performing shortest-path routing to a single given destination.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="destination"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology topology, string destination, SymbolicValue<TS>[] symbolics) :
    this(topology,
      topology.MapNodes(n =>
        n == destination ? Option.Create<BgpRoute>(new BgpRoute()) : Option.None<BgpRoute>()), symbolics)
  {
  }

  /// <summary>
  ///   Construct a network performing shortest-path routing; the initial values and convergence time
  ///   must be given.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="initialValues"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology topology,
    Dictionary<string, Zen<Option<BgpRoute>>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(topology,
    topology.MapEdges(_ => Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath)),
    Lang.Omap2<BgpRoute>(BgpRouteExtensions.Min),
    initialValues,
    symbolics)
  {
  }
}

public class AnnotatedSp<TS> : AnnotatedNetwork<Option<BgpRoute>, TS>
{
  /// <inheritdoc cref="AnnotatedNetwork{T,TS}(Timepiece.Networks.Network{T,TS},System.Collections.Generic.Dictionary{string,System.Func{ZenLib.Zen{T},ZenLib.Zen{System.Numerics.BigInteger},ZenLib.Zen{bool}}},System.Collections.Generic.IReadOnlyDictionary{string,System.Func{ZenLib.Zen{T},ZenLib.Zen{bool}}},System.Collections.Generic.IReadOnlyDictionary{string,System.Func{ZenLib.Zen{T},ZenLib.Zen{bool}}},System.Numerics.BigInteger)"/>
  public AnnotatedSp(Sp<TS> sp, Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties) : base(sp, annotations,
    stableProperties, safetyProperties, new BigInteger(4))
  {
  }
}

public class InferSp : Infer<Option<BgpRoute>>
{
  public InferSp(Sp<Unit> sp, IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<string, Func<Zen<Option<BgpRoute>>, Zen<bool>>> afterInvariants) :
    base(sp, beforeInvariants, afterInvariants)
  {
  }
}

/// <summary>
///   Static factory class for Sp networks.
/// </summary>
public static class Sp
{
  /// <summary>
  /// Return a new <c>Sp{Unit}</c> for a fat-tree topology routing to a concrete destination.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  private static Sp<Unit> ConcreteFatTreeSp(uint numPods, string destination)
  {
    return new Sp<Unit>(Topologies.FatTree(numPods), destination, Array.Empty<SymbolicValue<Unit>>());
  }

  /// <summary>
  ///   Return an Sp k-fattree network to check reachability of a single destination node, where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <param name="inferTimes"></param>
  /// <returns></returns>
  public static AnnotatedSp<Unit> Reachability(uint numPods, string destination, bool inferTimes)
  {
    var sp = ConcreteFatTreeSp(numPods, destination);
    // no safety property
    var safetyProperties = sp.Topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties = sp.Topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations;
    if (inferTimes)
    {
      var infer = new InferSp(sp, safetyProperties, stableProperties)
      {
        // specify a maximum time so that we ensure that the safety check still holds
        // in this case, the maximum must be the network's converge time
        MaxTime = new BigInteger(4)
      };
      annotations = infer.InferAnnotations(InferenceStrategy.SymbolicEnumeration);
    }
    else
    {
      var distances = sp.Topology.BreadthFirstSearch(destination);
      annotations =
        distances.Select(p => (n: p.Key, a: Lang.Finally<Option<BgpRoute>>(p.Value, Option.IsSome)))
          .ToDictionary(p => p.n, p => p.a);
    }

    return new AnnotatedSp<Unit>(sp, annotations, stableProperties, safetyProperties);
  }

  // slightly weaker path length property with simpler annotations
  public static AnnotatedSp<Unit> PathLengthNoSafety(uint numPods, string destination, bool inferTimes)
  {
    var sp = ConcreteFatTreeSp(numPods, destination);
    var distances = sp.Topology.BreadthFirstSearch(destination);

    var before = Lang.OrSome<BgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero));
    var afterConditions =
      distances.Select(p => (n: p.Key, after: Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(p.Value))))
        .ToDictionary(p => p.n, p => p.after);
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations;
    if (inferTimes)
    {
      var infer = new InferSp(sp, sp.Topology.MapNodes(_ => before), afterConditions)
      {
        // specify a maximum time so that we ensure that the safety check still holds
        // in this case, the maximum must be the network's converge time
        MaxTime = new BigInteger(4)
      };
      annotations = infer.InferAnnotations(InferenceStrategy.SymbolicEnumeration);
    }
    else
    {
      annotations =
        distances.Select(p =>
          {
            var after = Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(p.Value));
            return (n: p.Key, a: Lang.Until(p.Value, before, after));
          })
          .ToDictionary(p => p.n, p => p.a);
    }

    var stableProperties =
      sp.Topology.MapNodes(_ => Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = sp.Topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    return new AnnotatedSp<Unit>(sp, annotations, stableProperties, safetyProperties);
  }

  /// <summary>
  ///   Return an Sp k-fattree network to check path length of routes to a single destination node,
  ///   where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  public static AnnotatedSp<Unit> PathLength(uint numPods, string destination)
  {
    var sp = ConcreteFatTreeSp(numPods, destination);
    var distances = sp.Topology.BreadthFirstSearch(destination);

    var annotations =
      distances.Select(p => (p.Key, Lang.Until(p.Value,
          Lang.IsNone<BgpRoute>(),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(p.Value)))))
        .ToDictionary(p => p.Item1, p => p.Item2);

    var stableProperties =
      sp.Topology.MapNodes(_ => Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = sp.Topology.MapNodes(_ =>
      Lang.Union(Lang.IsNone<BgpRoute>(), Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    return new AnnotatedSp<Unit>(sp, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedSp<Pair<string, int>> AllPairsReachability(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    var symbolicValues = new SymbolicValue<Pair<string, int>>[] {dest};
    var sp = new Sp<Pair<string, int>>(topology, initialValues, symbolicValues);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var annotations = topology.MapNodes(n =>
      Lang.Finally(dest.SymbolicDistance(n, topology.L(n)), Lang.IsSome<BgpRoute>()));
    return new AnnotatedSp<Pair<string, int>>(sp, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedSp<Pair<string, int>> AllPairsPathLength(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    var symbolics = new SymbolicValue<Pair<string, int>>[] {dest};
    var sp = new Sp<Pair<string, int>>(topology, initialValues, symbolics);
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ =>
      Lang.Union(b => b.IsNone(),
        Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4)))));
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance,
          Lang.IsNone<BgpRoute>(),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(distance)));
      });
    return new AnnotatedSp<Pair<string, int>>(sp, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedSp<Pair<string, int>> AllPairsPathLengthNoSafety(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    var sp = new Sp<Pair<string, int>>(topology, initialValues,
      new SymbolicValue<Pair<string, int>>[] {dest});
    var stableProperties = topology.MapNodes(_ =>
      Lang.IfSome<BgpRoute>(b => b.LengthAtMost(new BigInteger(4))));
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var annotations =
      topology.MapNodes(n =>
      {
        var distance = dest.SymbolicDistance(n, topology.L(n));
        return Lang.Until(distance,
          Lang.OrSome<BgpRoute>(b => Zen.And(b.LpEquals(100), b.GetAsPathLength() >= BigInteger.Zero)),
          Lang.IfSome(BgpRouteExtensions.MaxLengthDefaultLp(distance)));
      });
    return new AnnotatedSp<Pair<string, int>>(sp, annotations, stableProperties, safetyProperties);
  }
}

using System.Numerics;
using MisterWolf;
using Timepiece.Networks;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Benchmarks;

public class Sp<TV, TS> : Network<Option<BgpRoute>, TV, TS> where TV : IEquatable<TV>
{
  /// <summary>
  ///   Construct a network performing shortest-path routing to a single given destination.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="destination"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology<TV> topology, TV destination, SymbolicValue<TS>[] symbolics) :
    this(topology,
      topology.MapNodes(n =>
        n.Equals(destination) ? Option.Create<BgpRoute>(new BgpRoute()) : Option.Null<BgpRoute>()), symbolics)
  {
  }

  /// <summary>
  ///   Construct a network performing shortest-path routing; the initial values and convergence time
  ///   must be given.
  /// </summary>
  /// <param name="topology"></param>
  /// <param name="initialValues"></param>
  /// <param name="symbolics"></param>
  public Sp(Topology<TV> topology,
    Dictionary<TV, Zen<Option<BgpRoute>>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(topology,
    topology.MapEdges(_ => Lang.Omap<BgpRoute, BgpRoute>(BgpRouteExtensions.IncrementAsPath)),
    Lang.Omap2<BgpRoute>(BgpRouteExtensions.Min),
    initialValues,
    symbolics)
  {
  }
}

public class AnnotatedSp<TV, TS> : AnnotatedNetwork<Option<BgpRoute>, TV, TS> where TV : IEquatable<TV>
{
  /// <inheritdoc cref="AnnotatedNetwork{T,TV,TS}(Timepiece.Networks.Network{T,TV,TS},System.Collections.Generic.Dictionary{TV,System.Func{ZenLib.Zen{T},ZenLib.Zen{System.Numerics.BigInteger},ZenLib.Zen{bool}}},System.Collections.Generic.IReadOnlyDictionary{TV,System.Func{ZenLib.Zen{T},ZenLib.Zen{bool}}},System.Collections.Generic.IReadOnlyDictionary{TV,System.Func{ZenLib.Zen{T},ZenLib.Zen{bool}}},System.Numerics.BigInteger)"/>
  public AnnotatedSp(Sp<TV, TS> sp, Dictionary<TV, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<TV, Func<Zen<Option<BgpRoute>>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<TV, Func<Zen<Option<BgpRoute>>, Zen<bool>>> safetyProperties) : base(sp, annotations,
    stableProperties, safetyProperties, new BigInteger(4))
  {
  }
}

public class InferSp<TV, TS> : Infer<Option<BgpRoute>, TV, TS> where TV : IEquatable<TV>
{
  public InferSp(Sp<TV, TS> sp, IReadOnlyDictionary<TV, Func<Zen<Option<BgpRoute>>, Zen<bool>>> beforeInvariants,
    IReadOnlyDictionary<TV, Func<Zen<Option<BgpRoute>>, Zen<bool>>> afterInvariants) :
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
  private static Sp<string, Unit> ConcreteFatTreeSp(uint numPods, string destination)
  {
    return new Sp<string, Unit>(Topologies.FatTree(numPods), destination, Array.Empty<SymbolicValue<Unit>>());
  }

  /// <summary>
  ///   Return an Sp k-fattree network to check reachability of a single destination node, where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <param name="inferTimes"></param>
  /// <returns></returns>
  public static AnnotatedSp<string, Unit> Reachability(uint numPods, string destination, bool inferTimes)
  {
    var sp = ConcreteFatTreeSp(numPods, destination);
    // no safety property
    var safetyProperties = sp.Topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var stableProperties = sp.Topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    Dictionary<string, Func<Zen<Option<BgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations;
    if (inferTimes)
    {
      var infer = new InferSp<string, Unit>(sp, safetyProperties, stableProperties)
      {
        // specify a maximum time so that we ensure that the safety check still holds
        // in this case, the maximum must be the network's converge time
        MaxTime = new BigInteger(4)
      };
      annotations = infer.InferAnnotationsWithStats(InferenceStrategy.SymbolicEnumeration);
    }
    else
    {
      var distances = sp.Topology.BreadthFirstSearch(destination);
      annotations =
        distances.Select(p => (n: p.Key, a: Lang.Finally<Option<BgpRoute>>(p.Value, Option.IsSome)))
          .ToDictionary(p => p.n, p => p.a);
    }

    return new AnnotatedSp<string, Unit>(sp, annotations, stableProperties, safetyProperties);
  }

  // slightly weaker path length property with simpler annotations
  public static AnnotatedSp<string, Unit> PathLengthNoSafety(uint numPods, string destination, bool inferTimes)
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
      var infer = new InferSp<string, Unit>(sp, sp.Topology.MapNodes(_ => before), afterConditions)
      {
        // specify a maximum time so that we ensure that the safety check still holds
        // in this case, the maximum must be the network's converge time
        MaxTime = new BigInteger(4)
      };
      annotations = infer.InferAnnotationsWithStats(InferenceStrategy.SymbolicEnumeration);
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
    return new AnnotatedSp<string, Unit>(sp, annotations, stableProperties, safetyProperties);
  }

  /// <summary>
  ///   Return an Sp k-fattree network to check path length of routes to a single destination node,
  ///   where k is the given numPods.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  public static AnnotatedSp<string, Unit> PathLength(uint numPods, string destination)
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
    return new AnnotatedSp<string, Unit>(sp, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedSp<string, Pair<string, int>> AllPairsReachability(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    var symbolicValues = new SymbolicValue<Pair<string, int>>[] {dest};
    var sp = new Sp<string, Pair<string, int>>(topology, initialValues, symbolicValues);
    var stableProperties = topology.MapNodes(_ => Lang.IsSome<BgpRoute>());
    var safetyProperties = topology.MapNodes(_ => Lang.True<Option<BgpRoute>>());
    var annotations = topology.MapNodes(n =>
      Lang.Finally(dest.SymbolicDistance(n, topology.L(n)), Lang.IsSome<BgpRoute>()));
    return new AnnotatedSp<string, Pair<string, int>>(sp, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedSp<string, Pair<string, int>> AllPairsPathLength(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    var symbolics = new SymbolicValue<Pair<string, int>>[] {dest};
    var sp = new Sp<string, Pair<string, int>>(topology, initialValues, symbolics);
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
    return new AnnotatedSp<string, Pair<string, int>>(sp, annotations, stableProperties, safetyProperties);
  }

  public static AnnotatedSp<string, Pair<string, int>> AllPairsPathLengthNoSafety(uint numPods)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var dest = new SymbolicDestination(topology);
    // set a node to be the destination if it matches the symbolic
    var initialValues =
      topology.MapNodes(n =>
        Option.Create<BgpRoute>(new BgpRoute()).Where(_ => dest.Equals(topology, n)));
    var sp = new Sp<string, Pair<string, int>>(topology, initialValues,
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
    return new AnnotatedSp<string, Pair<string, int>>(sp, annotations, stableProperties, safetyProperties);
  }
}

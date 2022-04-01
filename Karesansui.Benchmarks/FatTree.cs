using System.Numerics;
using Karesansui.Networks;
using ZenLib;

namespace Karesansui.Benchmarks;

public class FatTree<T, TS> : Network<T, TS>
{
  public uint NumPods { get; init; }

  /// <summary>
  /// Constructor specifying transfer on every edge.
  /// </summary>
  /// <param name="numPods">Number of pods in topology.</param>
  /// <param name="destination">The destination node.</param>
  /// <param name="transferFunction">Transfer function for each edge.</param>
  /// <param name="mergeFunction">Merge function for the network.</param>
  /// <param name="destinationRoute">The initial route at the destination node.</param>
  /// <param name="nullRoute">The initial route at non-destination nodes.</param>
  /// <param name="annotations">Annotations on nodes.</param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties">Safety property for each node.</param>
  /// <param name="symbolics">Symbolics.</param>
  public FatTree(uint numPods, string destination,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Zen<T> destinationRoute, Zen<T> nullRoute,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) :
    this(Topologies.FatTree(numPods), destination, transferFunction, mergeFunction, destinationRoute, nullRoute,
      annotations, stableProperties, safetyProperties, symbolics)
  {
    NumPods = numPods;
  }

  /// <summary>
  /// Constructor specifying a universal transfer function.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <param name="universalTransferFunction"></param>
  /// <param name="mergeFunction"></param>
  /// <param name="destinationRoute"></param>
  /// <param name="nullRoute"></param>
  /// <param name="annotations"></param>
  /// <param name="stableProperties"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="symbolics"></param>
  public FatTree(uint numPods, string destination,
    Func<Zen<T>, Zen<T>> universalTransferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Zen<T> destinationRoute, Zen<T> nullRoute,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) : this(
    Topologies.FatTree(numPods), destination, universalTransferFunction, mergeFunction, destinationRoute, nullRoute,
    annotations, stableProperties, safetyProperties, symbolics)
  {
    NumPods = numPods;
  }

  protected FatTree(Topology topology, string destination,
    Func<Zen<T>, Zen<T>> universalTransferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Zen<T> destinationRoute, Zen<T> nullRoute,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) : this(topology,
    destination, topology.ForAllEdges(_ => universalTransferFunction), mergeFunction, destinationRoute, nullRoute,
    annotations, stableProperties, safetyProperties, symbolics)
  {
    // there are (k/2)^2 core nodes for k pods
    NumPods = (uint) (2 * Math.Sqrt(topology.Neighbors.Keys.Count(n => n.StartsWith("core"))));
  }

  protected FatTree(Topology topology, string destination,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Zen<T> destinationRoute, Zen<T> nullRoute,
    Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<string, Func<Zen<T>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) : base(topology,
    transferFunction, mergeFunction,
    topology.ForAllNodes(n => n == destination ? destinationRoute : nullRoute),
    annotations,
    topology.ForAllNodes(n =>
      Lang.Intersect(Lang.Finally(new BigInteger(4), safetyProperties[n]), Lang.Globally(safetyProperties[n]))),
    topology.ForAllNodes(n => Lang.Intersect(safetyProperties[n], stableProperties[n])),
    symbolics)
  {
    // there are (k/2)^2 core nodes for k pods
    NumPods = (uint) (2 * Math.Sqrt(topology.Neighbors.Keys.Count(n => n.StartsWith("core"))));
  }
}

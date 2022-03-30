using System.Numerics;
using Karesansui.Networks;
using ZenLib;

namespace Karesansui.Benchmarks;

public class FatTree<TS> : Network<Option<BatfishBgpRoute>, TS>
{
  public uint NumPods { get; init; }

  /// <summary>
  /// Constructor specifying transfer on every edge.
  /// </summary>
  /// <param name="numPods">Number of pods in topology.</param>
  /// <param name="destination">The destination node.</param>
  /// <param name="transferFunction">Transfer function for each edge.</param>
  /// <param name="annotations">Annotations on nodes.</param>
  /// <param name="safetyProperties">Safety property for each node.</param>
  /// <param name="symbolics">Symbolics.</param>
  public FatTree(uint numPods, string destination,
    Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>> transferFunction,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties, SymbolicValue<TS>[] symbolics) :
    this(Topologies.FatTree(numPods), destination, transferFunction, annotations, safetyProperties, symbolics)
  {
    NumPods = numPods;
  }

  /// <summary>
  /// Constructor specifying a universal transfer function.
  /// </summary>
  /// <param name="numPods"></param>
  /// <param name="destination"></param>
  /// <param name="universalTransferFunction"></param>
  /// <param name="annotations"></param>
  /// <param name="safetyProperties"></param>
  /// <param name="symbolics"></param>
  public FatTree(uint numPods, string destination,
    Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>> universalTransferFunction,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties, SymbolicValue<TS>[] symbolics) : this(
    Topologies.FatTree(numPods), destination, universalTransferFunction, annotations, safetyProperties, symbolics)
  {
    NumPods = numPods;
  }

  protected FatTree(Topology topology, string destination,
    Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>> universalTransferFunction,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) : this(topology,
    destination, topology.ForAllEdges(_ => universalTransferFunction), annotations, safetyProperties, symbolics)
  {
    // there are (k/2)^2 core nodes for k pods
    NumPods = (uint) (2 * Math.Sqrt(topology.Neighbors.Keys.Count(n => n.StartsWith("core"))));
  }

  protected FatTree(Topology topology, string destination,
    Dictionary<(string, string), Func<Zen<Option<BatfishBgpRoute>>, Zen<Option<BatfishBgpRoute>>>> transferFunction,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<BatfishBgpRoute>>, Zen<bool>>> safetyProperties,
    SymbolicValue<TS>[] symbolics) : base(topology,
    transferFunction,
    Lang.Omap2<BatfishBgpRoute>(BatfishBgpRouteExtensions.Min),
    topology.ForAllNodes(n =>
      n == destination ? Option.Create<BatfishBgpRoute>(new BatfishBgpRoute()) : Option.None<BatfishBgpRoute>()),
    annotations,
    topology.ForAllNodes(n => Lang.Finally(new BigInteger(4), safetyProperties[n])), safetyProperties, symbolics)
  {
    // there are (k/2)^2 core nodes for k pods
    NumPods = (uint) (2 * Math.Sqrt(topology.Neighbors.Keys.Count(n => n.StartsWith("core"))));
  }
}

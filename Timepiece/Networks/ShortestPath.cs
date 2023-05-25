using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class ShortestPath<TV, TS> : Network<Option<BigInteger>, TV, TS>
{
  public ShortestPath(Topology<TV> topology, Dictionary<TV, Zen<Option<BigInteger>>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(topology, topology.MapEdges(_ => Lang.Omap(Lang.Incr(BigInteger.One))),
    Lang.Omap2<BigInteger>(Min), initialValues, symbolics)
  {
  }

  public ShortestPath(Topology<TV> topology, TV destination, SymbolicValue<TS>[] symbolics) : this(topology,
    topology.MapNodes(n =>
      n.Equals(destination) ? Option.Create<BigInteger>(BigInteger.Zero) : Option.Null<BigInteger>()), symbolics)
  {
  }
}

using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class ShortestPath<TS> : Network<Option<BigInteger>, TS>
{
  public ShortestPath(Topology topology, Dictionary<string, Zen<Option<BigInteger>>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(topology, topology.MapEdges(_ => Lang.Omap(Lang.Incr(BigInteger.One))),
    Lang.Omap2<BigInteger>(Min), initialValues, symbolics)
  {
  }
}

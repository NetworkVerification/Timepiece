using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class ShortestPath<TV, TS> : Network<Option<BigInteger>, TV, TS>
{
  public ShortestPath(Digraph<TV> digraph, Dictionary<TV, Zen<Option<BigInteger>>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(digraph, digraph.MapEdges(_ => Lang.Omap(Lang.Incr(BigInteger.One))),
    Lang.Omap2<BigInteger>(Min), initialValues, symbolics)
  {
  }

  public ShortestPath(Digraph<TV> digraph, TV destination, SymbolicValue<TS>[] symbolics) : this(digraph,
    digraph.MapNodes(n =>
      n.Equals(destination) ? Option.Create<BigInteger>(BigInteger.Zero) : Option.Null<BigInteger>()), symbolics)
  {
  }
}

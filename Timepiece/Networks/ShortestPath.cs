using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class ShortestPath<NodeType, SymbolicType> : Network<Option<BigInteger>, NodeType, SymbolicType>
{
  public ShortestPath(Digraph<NodeType> digraph, Dictionary<NodeType, Zen<Option<BigInteger>>> initialValues,
    SymbolicValue<SymbolicType>[] symbolics) : base(digraph,
    digraph.MapEdges(_ => Lang.Omap(Lang.Incr(BigInteger.One))),
    Lang.Omap2<BigInteger>(Min), initialValues, symbolics)
  {
  }

  public ShortestPath(Digraph<NodeType> digraph, NodeType destination, SymbolicValue<SymbolicType>[] symbolics) : this(
    digraph,
    digraph.MapNodes(n =>
      n.Equals(destination) ? Option.Create<BigInteger>(BigInteger.Zero) : Option.Null<BigInteger>()), symbolics)
  {
  }
}

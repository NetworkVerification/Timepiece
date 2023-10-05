using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

/// <summary>
/// A network with a shortest-paths routing algebra, using unbounded integers.
/// </summary>
/// <typeparam name="NodeType">The type of nodes.</typeparam>
public class ShortestPath<NodeType> : Network<Option<BigInteger>, NodeType>
{
  public ShortestPath(Digraph<NodeType> digraph, Dictionary<NodeType, Zen<Option<BigInteger>>> initialValues,
    ISymbolic[] symbolics) : base(digraph,
    digraph.MapEdges(_ => Lang.Omap(Lang.Incr(BigInteger.One))),
    Lang.Omap2<BigInteger>(Min), initialValues, symbolics)
  {
  }

  public ShortestPath(Digraph<NodeType> digraph, NodeType destination, ISymbolic[] symbolics) : this(
    digraph,
    digraph.MapNodes(n =>
      n.Equals(destination) ? Option.Create<BigInteger>(BigInteger.Zero) : Option.Null<BigInteger>()), symbolics)
  {
  }
}

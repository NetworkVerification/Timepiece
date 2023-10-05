using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Tests.Networks;

public class UnitNetwork<NodeType> : Network<Unit, NodeType> where NodeType : notnull
{
  public UnitNetwork(Digraph<NodeType> digraph, ISymbolic[] symbolics) : base(digraph,
    digraph.MapEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    digraph.MapNodes<Zen<Unit>>(_ => new Unit()), symbolics)
  {
  }
}

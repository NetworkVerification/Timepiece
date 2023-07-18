using ZenLib;

namespace Timepiece.Networks;

public class UnitNetwork<TV, TS> : Network<Unit, TV, TS>
{
  public UnitNetwork(Digraph<TV> digraph, SymbolicValue<TS>[] symbolics) : base(digraph,
    digraph.MapEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    digraph.MapNodes<Zen<Unit>>(_ => new Unit()), symbolics)
  {
  }
}

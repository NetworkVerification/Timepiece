using ZenLib;

namespace Timepiece.Networks;

public class UnitNetwork<TV, TS> : Network<Unit, TV, TS>
{
  public UnitNetwork(Topology<TV> topology, SymbolicValue<TS>[] symbolics) : base(topology,
    topology.MapEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    topology.MapNodes<Zen<Unit>>(_ => new Unit()), symbolics)
  {
  }
}

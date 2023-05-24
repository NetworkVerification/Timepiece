using ZenLib;

namespace Timepiece.Networks;

public class UnitNetwork<TS> : Network<Unit, TS>
{
  public UnitNetwork(Topology topology, SymbolicValue<TS>[] symbolics) : base(topology,
    topology.MapEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    topology.MapNodes<Zen<Unit>>(_ => new Unit()), symbolics)
  {
  }
}

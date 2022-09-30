using System;
using ZenLib;

namespace Timepiece.Networks;

public class UnitNetwork : Network<Unit, object>
{
  public UnitNetwork(Topology topology) : base(topology,
    topology.MapEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    topology.MapNodes<Zen<Unit>>(_ => new Unit()),
    topology.MapNodes(_ => Lang.Globally(UnitPredicate())),
    topology.MapNodes(_ => Lang.Globally(UnitPredicate())),
    topology.MapNodes(_ => UnitPredicate()), System.Array.Empty<SymbolicValue<object>>())
  {
  }

  private static Func<Zen<Unit>, Zen<bool>> UnitPredicate()
  {
    return _ => true;
  }
}

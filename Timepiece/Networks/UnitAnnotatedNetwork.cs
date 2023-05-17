using System;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Networks;

public class UnitAnnotatedNetwork : AnnotatedNetwork<Unit, object>
{
  public UnitAnnotatedNetwork(Topology topology) : base(topology,
    topology.MapEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    topology.MapNodes<Zen<Unit>>(_ => new Unit()),
    topology.MapNodes(_ => Lang.Globally(UnitPredicate())),
    topology.MapNodes(_ => Lang.Globally(UnitPredicate())),
    topology.MapNodes(_ => UnitPredicate()), Array.Empty<SymbolicValue<object>>())
  {
  }

  private static Func<Zen<Unit>, Zen<bool>> UnitPredicate()
  {
    return _ => true;
  }
}

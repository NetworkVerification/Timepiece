using System;
using ZenLib;

namespace Timepiece.Networks;

public class UnitNetwork : Network<Unit, object>
{
  public UnitNetwork(Topology topology) : base(topology,
    topology.ForAllEdges(_ => Lang.Identity<Unit>()), (_, _) => new Unit(),
    topology.ForAllNodes<Zen<Unit>>(_ => new Unit()),
    topology.ForAllNodes(_ => Lang.Globally(UnitPredicate())),
    topology.ForAllNodes(_ => Lang.Globally(UnitPredicate())),
    topology.ForAllNodes(_ => UnitPredicate()), Array.Empty<SymbolicValue<object>>())
  {
  }

  private static Func<Zen<Unit>, Zen<bool>> UnitPredicate()
  {
    return _ => true;
  }
}

using System.Collections.Generic;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   A network with a boolean routing algebra.
/// </summary>
public class BooleanNetwork<TV, TS> : Network<bool, TV, TS>
{
  public BooleanNetwork(Topology<TV> topology, Dictionary<TV, Zen<bool>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(topology, topology.MapEdges(_ => Lang.Identity<bool>()), Zen.Or,
    initialValues, symbolics)
  {
  }
}

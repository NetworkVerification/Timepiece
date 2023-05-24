using System.Collections.Generic;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   A network with a boolean routing algebra.
/// </summary>
public class BooleanNetwork<TS> : Network<bool, TS>
{
  public BooleanNetwork(Topology topology, Dictionary<string, Zen<bool>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(topology, topology.MapEdges(_ => Lang.Identity<bool>()), Zen.Or,
    initialValues, symbolics)
  {
  }
}

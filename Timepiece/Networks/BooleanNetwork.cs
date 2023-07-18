using System.Collections.Generic;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   A network with a boolean routing algebra.
/// </summary>
public class BooleanNetwork<TV, TS> : Network<bool, TV, TS>
{
  public BooleanNetwork(Digraph<TV> digraph, Dictionary<TV, Zen<bool>> initialValues,
    SymbolicValue<TS>[] symbolics) : base(digraph, digraph.MapEdges(_ => Lang.Identity<bool>()), Zen.Or,
    initialValues, symbolics)
  {
  }
}

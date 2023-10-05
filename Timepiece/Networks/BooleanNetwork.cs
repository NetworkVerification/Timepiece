using System.Collections.Generic;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   A network with a boolean routing algebra.
/// </summary>
public class BooleanNetwork<NodeType> : Network<bool, NodeType>
{
  public BooleanNetwork(Digraph<NodeType> digraph, Dictionary<NodeType, Zen<bool>> initialValues,
    ISymbolic[] symbolics) : base(digraph, digraph.MapEdges(_ => Lang.Identity<bool>()), Zen.Or,
    initialValues, symbolics)
  {
  }
}

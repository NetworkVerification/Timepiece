using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.Networks;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests.Networks;

/// <summary>
///   Network "functor" lifting a network to a network where links may fail.
/// </summary>
/// <typeparam name="RouteType"></typeparam>
/// <typeparam name="NodeType"></typeparam>
public class FaultTolerance<RouteType, NodeType> : AnnotatedNetwork<Option<RouteType>, NodeType>
  where NodeType : notnull
{
  public FaultTolerance(Network<RouteType, NodeType> net,
    Dictionary<NodeType, Zen<Option<RouteType>>> initialValues,
    Func<SymbolicValue<(NodeType, NodeType)>[],
        Dictionary<NodeType, Func<Zen<Option<RouteType>>, Zen<BigInteger>, Zen<bool>>>>
      annotations,
    Dictionary<NodeType, Func<Zen<Option<RouteType>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<Option<RouteType>>, Zen<bool>>> monolithicProperties, uint numFailed) : base(
    net.Digraph,
    new Dictionary<(NodeType, NodeType), Func<Zen<Option<RouteType>>, Zen<Option<RouteType>>>>(),
    Lang.Omap2(net.MergeFunction),
    initialValues,
    new Dictionary<NodeType, Func<Zen<Option<RouteType>>, Zen<BigInteger>, Zen<bool>>>(), modularProperties,
    monolithicProperties,
    net.Symbolics)
  {
    var failureSymbolics = CreateSymbolics(net.Digraph, numFailed);
    Annotations = annotations(failureSymbolics);
    TransferFunction = Transfer(net.TransferFunction, failureSymbolics);
    // add the failure symbolics on
    Symbolics = Symbolics.Concat(failureSymbolics).ToArray();
  }

  /// <summary>
  ///   Return true if the given edge is in the sequence of failed edges, and false otherwise.
  /// </summary>
  /// <param name="failedEdges"></param>
  /// <param name="edge"></param>
  /// <returns></returns>
  public static Zen<bool> IsFailed(IEnumerable<SymbolicValue<(NodeType, NodeType)>> failedEdges,
    (NodeType, NodeType) edge)
  {
    return failedEdges.Aggregate(False(), (current, e) => Or(current, e.EqualsValue(edge)));
  }

  private static Zen<bool> EdgeInNetwork(Digraph<NodeType> digraph, Zen<(NodeType, NodeType)> edge)
  {
    return digraph.FoldEdges(False(), (b, e) => Or(b, Constant(e) == edge));
  }

  private static SymbolicValue<(NodeType, NodeType)>[] CreateSymbolics(Digraph<NodeType> digraph, uint numFailed)
  {
    var symbolics = new SymbolicValue<(NodeType, NodeType)>[numFailed];
    for (var i = 0; i < numFailed; i++)
      symbolics[i] = new SymbolicValue<(NodeType, NodeType)>($"e{i}", e => EdgeInNetwork(digraph, e));

    return symbolics;
  }

  private static Dictionary<(NodeType, NodeType), Func<Zen<Option<RouteType>>, Zen<Option<RouteType>>>> Transfer(
    IReadOnlyDictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> inner,
    SymbolicValue<(NodeType, NodeType)>[] failedEdges)
  {
    var lifted = new Dictionary<(NodeType, NodeType), Func<Zen<Option<RouteType>>, Zen<Option<RouteType>>>>();
    foreach (var (edge, f) in inner)
      lifted[edge] =
        Lang.Test(_ => IsFailed(failedEdges, edge), Lang.Const(Option.None<RouteType>()), Lang.Omap(f));

    return lifted;
  }
}

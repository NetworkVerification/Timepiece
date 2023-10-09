using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace Timepiece;

/// <summary>
/// Helper functions for assigning symbolic times to fat-tree nodes.
/// </summary>
public static class FatTreeSymbolicTimes
{
  /// <summary>
  /// Return a mapping over the nodes in <paramref name="g"/> where each node has a Finally annotation
  /// with a witness time chosen from <paramref name="symbolicTimes"/> according to the node's distance
  /// from the <paramref name="destination"/> node, and with the predicate <paramref name="afterPredicate"/>.
  /// </summary>
  /// <param name="g"></param>
  /// <param name="destination"></param>
  /// <param name="afterPredicate"></param>
  /// <param name="symbolicTimes"></param>
  /// <typeparam name="RouteType"></typeparam>
  /// <returns></returns>
  public static Dictionary<string, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>>
    FinallyAnnotations<RouteType>(NodeLabelledDigraph<string, int> g, string destination,
      Func<Zen<RouteType>, Zen<bool>> afterPredicate, IReadOnlyList<Zen<BigInteger>> symbolicTimes) =>
    g.MapNodes(n =>
    {
      var dist = n.DistanceFromDestinationEdge(g.L(n), destination, g.L(destination));
      return Lang.Finally(symbolicTimes[dist], afterPredicate);
    });

  public static Dictionary<string, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>>
    FinallyAnnotations<RouteType>(NodeLabelledDigraph<string, int> g, SymbolicDestination destination,
      Func<Zen<RouteType>, Zen<bool>> afterPredicate, IReadOnlyList<Zen<BigInteger>> symbolicTimes) =>
    g.MapNodes(n => Lang.Finally(destination.SymbolicDistanceCases(n, g.L(n), symbolicTimes), afterPredicate));
}

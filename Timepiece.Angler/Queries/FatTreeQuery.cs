using System.Numerics;
using ZenLib;
using Regex = System.Text.RegularExpressions.Regex;

namespace Timepiece.Angler.Queries;

/// <summary>
/// Queries for checking properties of fat-tree networks.
/// </summary>
public static partial class FatTreeQuery
{
  [System.Text.RegularExpressions.GeneratedRegex(@"(edge|aggregation|core)-(\d*)")]
  private static partial Regex FatTreeNodePattern();

  /// <summary>
  /// Infer the pod labels on a fat-tree digraph.
  /// A k-fat-tree has (k^2)/2 core nodes, k^2 aggregation nodes and k^2 edge nodes.
  /// </summary>
  /// <param name="digraph"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException">If a node in the digraph does not have a name matching </exception>
  public static NodeLabelledDigraph<string, int> LabelFatTree(Digraph<string> digraph)
  {
    // the #nodes = 5/4 k^2 where k is the number of pods, so k = sqrt(#nodes * 4/5)
    var numberOfPods = (int) Math.Floor(Math.Sqrt(digraph.NNodes * 0.8));
    var coreNodes = digraph.Nodes.Count(s => s.IsCore());
    var labels = digraph.MapNodes(n =>
    {
      var match = FatTreeNodePattern().Match(n);
      if (!match.Success) throw new ArgumentException($"Given node {n} does not match the fat-tree node pattern!");
      var nodeNumber = int.Parse(match.Groups[2].Value);
      // if the node is a core node, then the groups are defined as the maximum pod number plus the core node's number
      if (match.Groups[1].Value.IsCore()) return numberOfPods + nodeNumber;
      // for aggregation and edge nodes, the pod number is going to be the quotient of
      // dividing the node number minus the number of core nodes by the number of pods:
      // pod # of node = (node # - #core) / #pods
      return (nodeNumber - coreNodes) / numberOfPods;
    });

    return new NodeLabelledDigraph<string, int>(digraph, labels);
  }

  public static NetworkQuery<RouteEnvironment, string> Reachable(NodeLabelledDigraph<string, int> digraph)
  {
    // infer the destination as the last edge node
    var destination = FatTree.FatTreeLayer.Edge.Node((uint) digraph.NNodes - 1);
    var initialRoutes =
      digraph.MapNodes(n =>
        n.Equals(destination) ? Zen.Constant(new RouteEnvironment()).WithResultValue(true) : new RouteEnvironment());
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => RouteEnvironmentExtensions.GetResultValue);
    var modularProperties = digraph.MapNodes(n => Lang.Finally(lastTime, monolithicProperties[n]));
    var annotations = FatTreeSymbolicTimes.FinallyAnnotations<RouteEnvironment>(digraph, destination,
      r => r.GetResultValue(), symbolicTimes.Select(s => s.Value).ToList());

    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolicTimes.Cast<ISymbolic>().ToArray(),
      monolithicProperties, modularProperties, annotations);
  }

  public static NetworkQuery<RouteEnvironment, string> MaxPathLength(NodeLabelledDigraph<string, int> digraph)
  {
    // infer the destination as the last edge node
    var destination = FatTree.FatTreeLayer.Edge.Node((uint) digraph.NNodes - 1);
    var destinationPod = digraph.L(destination);
    var initialRoutes =
      digraph.MapNodes(n =>
        n.Equals(destination) ? Zen.Constant(new RouteEnvironment()).WithResultValue(true) : new RouteEnvironment());
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => r => r.GetAsPathLength() <= new BigInteger(4));
    var modularProperties = digraph.MapNodes(n => Lang.Finally(lastTime, monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
    {
      var dist = n.DistanceFromDestinationEdge(digraph.L(n), destination, destinationPod);
      var time = symbolicTimes[dist].Value;
      var maxPathLength = new BigInteger(dist);

      var safety =
        Lang.Globally<RouteEnvironment>(r =>
          Zen.Implies(r.GetResultValue(),
            Zen.And(r.GetAsPathLength() >= BigInteger.Zero, r.GetLp() == RouteEnvironment.DefaultLp,
              r.GetWeight() == RouteEnvironment.DefaultWeight)));
      return Lang.Intersect(safety,
        Lang.Finally<RouteEnvironment>(time, r => Zen.And(r.GetResultValue(), r.GetAsPathLength() <= maxPathLength)));
    });

    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolicTimes.Cast<ISymbolic>().ToArray(),
      monolithicProperties, modularProperties, annotations);
  }

  public static NetworkQuery<RouteEnvironment, string> ValleyFreedom(NodeLabelledDigraph<string, int> digraph)
  {
    // infer the destination as the last edge node
    var destination = FatTree.FatTreeLayer.Edge.Node((uint) digraph.NNodes - 1);
    var destinationPod = digraph.L(destination);
    var initialRoutes =
      digraph.MapNodes(n =>
        n.Equals(destination) ? Zen.Constant(new RouteEnvironment()).WithResultValue(true) : new RouteEnvironment());
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => r => r.GetResultValue());
    var modularProperties = digraph.MapNodes(n => Lang.Finally(lastTime, monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
    {
      var indexTime = n.DistanceFromDestinationEdge(digraph.L(n), destination, destinationPod);
      var witnessTime = symbolicTimes[indexTime].Value;
      // (1) We need the precise best path length here as dist can be arbitrarily larger than the path length,
      //     and we need to ensure that nodes never have routes with path length larger than their best.
      var bestPathLength = new BigInteger(indexTime);
      // (2) At all times, either a node has no route, or it has a route with the default LP
      //     and that has path length no *better* than the best path length.
      var safety =
        Lang.Globally<RouteEnvironment>(
          r => Zen.Implies(r.GetResultValue(),
            Zen.And(r.GetLp() == RouteEnvironment.DefaultLp, r.GetWeight() == RouteEnvironment.DefaultWeight,
              // (2a) If we drop this constraint, then a node "further" along the path may send a better route that violates
              //      the eventually constraint that the path length be equal.
              r.GetAsPathLength() >= bestPathLength)));
      // (3) Eventually, nodes close to the destination must not be tagged down.
      //     All nodes eventually have routes equal to their best path length.
      var eventually =
        Lang.Finally<RouteEnvironment>(witnessTime,
          r =>
            Zen.And(r.GetResultValue(),
              // core and close-to-destination nodes should not be tagged down
              // TODO: get the appropriate community tag
              Zen.Implies(Zen.Or(bestPathLength < new BigInteger(2)), Zen.Not(r.HasCommunity("FOO"))),
              // (3a) If we let the eventual path length be any better than the best, then we can have an inductive condition
              //      violation at the aggregation nodes. A "further" core node (above) can send a better route than a
              //      "closer" edge node below, causing DownTag to be set and violating the inductive condition.
              r.GetAsPathLength() == bestPathLength));
      return Lang.Intersect(safety, eventually);
    });

    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolicTimes.Cast<ISymbolic>().ToArray(),
      monolithicProperties, modularProperties, annotations);
  }
}

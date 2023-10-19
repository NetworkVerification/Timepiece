using System.Numerics;
using System.Text.RegularExpressions;
using ZenLib;
using Regex = System.Text.RegularExpressions.Regex;

namespace Timepiece.Angler.Queries;

/// <summary>
///   Queries for checking properties of fat-tree networks.
/// </summary>
public static partial class FatTreeQuery
{
  [GeneratedRegex(@"(edge|aggregation|core)-(\d*)")]
  private static partial Regex FatTreeNodePattern();

  /// <summary>
  ///   Infer the pod labels on a fat-tree digraph.
  ///   A k-fat-tree has (k^2)/2 core nodes, k^2 aggregation nodes and k^2 edge nodes.
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

  /// <summary>
  ///   Return the second (dropping) community associated with the given node.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="node"></param>
  /// <returns></returns>
  private static string DropCommunity(NodeLabelledDigraph<string, int> digraph, string node)
  {
    return node.IsAggregation() ? $"4:{digraph.L(node)}" : node.IsEdge() ? $"5:{digraph.L(node)}" : "6:0";
  }

  private static IEnumerable<string> DropCommunities(int numberOfPods)
  {
    // Enumerable.Range takes a start and a *count*, not a max -- so we want the number of pods
    return Enumerable.Range(0, numberOfPods)
      .SelectMany(podNumber => new[] {$"4:{podNumber}", $"5:{podNumber}"})
      .Concat(Enumerable.Repeat("6:0", 1));
  }

  public static NetworkQuery<RouteEnvironment, string> ValleyFreedom(NodeLabelledDigraph<string, int> digraph)
  {
    // infer the destination as the last edge node
    var destination = FatTree.FatTreeLayer.Edge.Node((uint) digraph.NNodes - 1);
    var destinationPod = digraph.L(destination);
    var numberOfPods = destinationPod + 1;
    var dropCommunities = DropCommunities(numberOfPods).ToArray();

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
      // (2) At all times, either a node has no route, or it has a route with the default LP and weight
      //     and no drop communities
      var safety =
        Lang.Globally<RouteEnvironment>(
          r => Zen.Implies(r.GetResultValue(),
            Zen.And(r.GetLp() == RouteEnvironment.DefaultLp, r.GetWeight() == RouteEnvironment.DefaultWeight,
              // no negative path lengths
              r.GetAsPathLength() >= BigInteger.Zero,
              // no route should have any community indicating it has taken a valley and should be dropped
              Zen.And(dropCommunities.Select(c => Zen.Not(r.HasCommunity(c)))))));
      // (3) All nodes eventually have routes no worse than their best path length.
      var eventually =
        Lang.Finally<RouteEnvironment>(witnessTime,
          r =>
            Zen.And(r.GetResultValue(),
              r.GetAsPathLength() <= bestPathLength));
      return Lang.Intersect(safety, eventually);
    });

    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolicTimes.Cast<ISymbolic>().ToArray(),
      monolithicProperties, modularProperties, annotations);
  }
}

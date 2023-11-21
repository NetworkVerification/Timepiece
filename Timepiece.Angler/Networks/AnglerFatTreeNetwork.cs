using System.Numerics;
using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler.Networks;

/// <summary>
///   Checks for properties of fat-tree networks.
/// </summary>
public class AnglerFatTreeNetwork<RouteType> : AnnotatedNetwork<RouteType, string>
{
  public AnglerFatTreeNetwork(Digraph<string> digraph,
    Dictionary<(string, string), Func<Zen<RouteType>, Zen<RouteType>>> transferFunctions,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<string, Zen<RouteType>> initialValues,
    Dictionary<string, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties, ISymbolic[] symbolics) : base(digraph,
    transferFunctions, mergeFunction, initialValues, annotations, modularProperties, monolithicProperties, symbolics)
  {
  }

  private static string LastEdgeNode(Digraph<string> digraph)
  {
    return digraph.Nodes.Order().Last(n => n.IsEdge());
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> Reachable(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
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

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional,
      initialRoutes, annotations, modularProperties,
      monolithicProperties, symbolicTimes.Cast<ISymbolic>().ToArray());
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> MaxPathLength(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
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

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional,
      initialRoutes, annotations, modularProperties,
      monolithicProperties, symbolicTimes.Cast<ISymbolic>().ToArray());
  }

  /// <summary>
  /// Return an enumerable over all communities in the fat-tree that indicate a route should be dropped.
  /// </summary>
  /// <param name="numberOfPods">The number of pods in the fat-tree.</param>
  /// <returns>An enumerable of community tags.</returns>
  private static IEnumerable<string> DropCommunities(int numberOfPods)
  {
    return Enumerable.Range(0, numberOfPods)
      .SelectMany(podNumber => new[] {$"4:{podNumber}", $"5:{podNumber}"})
      .Concat(Enumerable.Repeat("6:0", 1));
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> ValleyFreedom(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
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

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional,
      initialRoutes, annotations, modularProperties,
      monolithicProperties, symbolicTimes.Cast<ISymbolic>().ToArray());
  }

  public static AnglerFatTreeNetwork<Pair<RouteEnvironment, bool>> FatTreeHijackFiltering(
    NodeLabelledDigraph<string, int> digraph,
    IEnumerable<string> externalPeers,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions,
    IReadOnlyDictionary<string, List<Ipv4Prefix>> prefixes)
  {
    // external neighbors send symbolic routes
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<RouteEnvironment>("external-route", externalPeers);
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
    var destinationPrefix = new SymbolicValue<Ipv4Prefix>("destination-prefix",
      p1 => prefixes[destination].Exists(p2 => p1 == p2));
    var initialRoutes =
      digraph.MapNodes(n =>
        externalRoutes.TryGetValue(n, out var externalRoute)
          // external neighbors start with routes with the ghost boolean variable set to true
          ? Pair.Create(externalRoute.Value, Zen.True())
          // internal nodes start with routes with the ghost boolean variable set to false
          : Pair.Create(
            Zen.Constant(new RouteEnvironment()).WithPrefix(destinationPrefix.Value)
              .WithResultValue(n.Equals(destination)),
            Zen.False()));
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = symbolicTimes[^1].Value;

    // lift the original transfer and merge to pairs
    var liftedTransferFunctions = digraph.MapEdges(e => Lang.Product(transferFunctions[e], Lang.Identity<bool>()));
    var liftedMerge =
      Lang.MergeBy<Pair<RouteEnvironment, bool>, RouteEnvironment>(
        (r1, r2) => MinOptionalForPrefix(r1, r2, destinationPrefix.Value),
        r => r.Item1());

    // the key property: nodes have routes for the destination prefix, and those routes are internal
    var mapInternal = AndIsInternal(r => Zen.And(r.GetResultValue(), r.GetPrefix() == destinationPrefix.Value));
    var monolithicProperties =
      digraph.MapNodes(n => externalRoutes.ContainsKey(n)
        ? Lang.True<Pair<RouteEnvironment, bool>>()
        : mapInternal);
    var modularProperties = digraph.MapNodes(n =>
      externalRoutes.ContainsKey(n)
        ? Lang.Globally(monolithicProperties[n])
        : Lang.Finally(lastTime, monolithicProperties[n]));
    // eventually, every node should have a route to the destination
    // NOTE: since the digraph contains the external peers, this will construct superfluous annotations for them
    // (we won't use those annotations, due to the check below ensuring that external peers' annotations are just G(true))
    var eventuallyAnnotations = FatTreeSymbolicTimes.FinallyAnnotations(digraph,
      destination, mapInternal, symbolicTimes.Select(s => s.Value).ToList());
    var annotations = digraph.MapNodes(n =>
    {
      if (externalRoutes.ContainsKey(n))
        return modularProperties[n];

      // if the route has a value with the prefix, then it must not be external
      var safety = Lang.Globally<Pair<RouteEnvironment, bool>>(r =>
        Zen.Implies(Zen.And(r.Item1().GetResultValue(), r.Item1().GetPrefix() == destinationPrefix.Value),
          Zen.Not(r.Item2())));
      return Lang.Intersect(safety, eventuallyAnnotations[n]);
    });

    // collect all the symbolics together
    var symbolics = new ISymbolic[] {destinationPrefix}.Concat(externalRoutes.Values).Concat(symbolicTimes).ToArray();
    return new AnglerFatTreeNetwork<Pair<RouteEnvironment, bool>>(digraph, liftedTransferFunctions, liftedMerge,
      initialRoutes,
      annotations,
      modularProperties, monolithicProperties, symbolics);
  }

  /// <summary>
  /// Return the minimum route of two routes modulo the given <paramref name="prefix"/>:
  /// if one of the two routes is <i>not</i> for the desired prefix, it should be ignored.
  /// </summary>
  /// <param name="r1">A route.</param>
  /// <param name="r2">A route.</param>
  /// <param name="prefix">The desired prefix.</param>
  /// <returns>The minimum route between <paramref name="r1"/> and <paramref name="r2"/>.</returns>
  private static Zen<RouteEnvironment> MinOptionalForPrefix(Zen<RouteEnvironment> r1, Zen<RouteEnvironment> r2,
    Zen<Ipv4Prefix> prefix)
  {
    return Zen.If(r1.GetPrefix() != prefix, r2, Zen.If(r2.GetPrefix() != prefix, r1, r1.MinOptional(r2)));
  }

  private static Func<Zen<Pair<RouteEnvironment, bool>>, Zen<bool>> AndIsInternal(
    Func<Zen<RouteEnvironment>, Zen<bool>> routePredicate) =>
    Lang.Both<RouteEnvironment, bool>(routePredicate, Zen.Not);
}

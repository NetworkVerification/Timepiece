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

  public AnglerFatTreeNetwork(Digraph<string> digraph,
    Dictionary<(string, string), Func<Zen<RouteType>, Zen<RouteType>>> transferFunctions,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<string, Zen<RouteType>> initialValues,
    Dictionary<string, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties, IEnumerable<ISymbolic> symbolics,
    IReadOnlyList<SymbolicTime> times) : this(digraph, transferFunctions, mergeFunction, initialValues, annotations,
    digraph.MapNodes(n => Lang.Finally(times[^1].Value, monolithicProperties[n])), monolithicProperties,
    symbolics.Concat(times).ToArray())
  {
  }
}

public static class AnglerFatTreeNetwork
{
  public static string LastEdgeNode(Digraph<string> digraph)
  {
    return digraph.Nodes.Where(n => n.IsEdge()).OrderBy(FatTree.IntNodeIndex).Last();
  }

  /// <summary>
  /// Return a mapping from nodes to routes, where a fixed destination node has an initial route and all others do not.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  private static Dictionary<string, Zen<RouteEnvironment>> InitialRoutes(Digraph<string> digraph, string destination) =>
    digraph.MapNodes(n => Zen.Constant(new RouteEnvironment()).WithResultValue(n.Equals(destination)));

  /// <summary>
  /// Return a mapping from nodes to routes, where a symbolic destination node has an initial route and all others do not.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  private static Dictionary<string, Zen<RouteEnvironment>> InitialRoutes(NodeLabelledDigraph<string, int> digraph,
    SymbolicDestination destination) =>
    digraph.MapNodes(n => Zen.Constant(new RouteEnvironment()).WithResultValue(destination.EqualsDigraph(digraph, n)));

  public static AnglerFatTreeNetwork<RouteEnvironment> Reachable(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
    var initialRoutes = InitialRoutes(digraph, destination);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => RouteEnvironmentExtensions.GetResultValue);
    var annotations = FatTreeSymbolicTimes.FinallyAnnotations<RouteEnvironment>(digraph, destination,
      r => r.GetResultValue(), symbolicTimes.Select(s => s.Value).ToList());

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional, initialRoutes, annotations, monolithicProperties,
      System.Array.Empty<ISymbolic>(), symbolicTimes);
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> ReachableAllToR(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    var destination = new SymbolicDestination(digraph);
    var initialValues = InitialRoutes(digraph, destination);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => RouteEnvironmentExtensions.GetResultValue);
    var annotations = FatTreeSymbolicTimes.FinallyAnnotations<RouteEnvironment>(digraph, destination,
      r => r.GetResultValue(),
      symbolicTimes.Select(s => s.Value).ToList());

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional,
      initialValues, annotations, monolithicProperties, new ISymbolic[] {destination}, symbolicTimes);
  }

  /// <summary>
  /// Return a Zen value encoding that a route must have a non-negative AS path length, a default local preference and a default weight.
  /// </summary>
  /// <param name="r">A route.</param>
  /// <returns>A Zen boolean.</returns>
  private static Zen<bool> DefaultLpWeightPath(Zen<RouteEnvironment> r) =>
    Zen.And(r.GetAsPathLength() >= BigInteger.Zero,
      r.GetLp() == RouteEnvironment.DefaultLp, r.GetWeight() == RouteEnvironment.DefaultWeight);

  private static Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>> MaxPathLength(Zen<BigInteger> time,
    Zen<BigInteger> maxPathLength)
  {
    var safety = Lang.Globally(RouteEnvironment.IfValue(DefaultLpWeightPath));
    return Lang.Intersect(safety,
      Lang.Finally<RouteEnvironment>(time, r => Zen.And(r.GetResultValue(), r.GetAsPathLength() <= maxPathLength)));
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> MaxPathLength(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
    var destinationPod = digraph.L(destination);
    var initialRoutes = InitialRoutes(digraph, destination);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => r => r.GetAsPathLength() <= new BigInteger(4));
    var annotations = digraph.MapNodes(n =>
    {
      var dist = n.DistanceFromDestinationEdge(digraph.L(n), destination, destinationPod);
      var time = symbolicTimes[dist].Value;
      var maxPathLength = new BigInteger(dist);
      return MaxPathLength(time, maxPathLength);
    });

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional, initialRoutes, annotations, monolithicProperties,
      System.Array.Empty<ISymbolic>(), symbolicTimes);
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> MaxPathLengthAllToR(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    var destination = new SymbolicDestination(digraph);
    var initialValues = InitialRoutes(digraph, destination);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => r => r.GetAsPathLength() <= new BigInteger(4));
    var annotations = digraph.MapNodes(n =>
    {
      var witnessTime = destination.SymbolicDistanceCases(n, digraph.L(n), symbolicTimes.Select(t => t.Value).ToList());
      var maxPathLength = destination.SymbolicDistance(n, digraph.L(n));
      return MaxPathLength(witnessTime, maxPathLength);
    });

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional, initialValues,
      annotations, monolithicProperties, new[] {destination}, symbolicTimes);
  }

  /// <summary>
  /// Return an enumerable over all communities in the fat-tree that indicate a route should be dropped.
  /// </summary>
  /// <param name="digraph">The fat-tree digraph.</param>
  /// <returns>An enumerable of community tags.</returns>
  private static IEnumerable<string> DropCommunities(NodeLabelledDigraph<string, int> digraph)
  {
    var numberOfPods = digraph.Labels.Values.Max() + 1;
    return Enumerable.Range(0, numberOfPods)
      .SelectMany(podNumber => new[] {$"4:{podNumber}", $"5:{podNumber}"})
      .Concat(Enumerable.Repeat("6:0", 1));
  }

  private static Zen<bool> NoDropCommunities(this Zen<RouteEnvironment> r, NodeLabelledDigraph<string, int> digraph) =>
    DropCommunities(digraph).ForAll(c => Zen.Not(r.HasCommunity(c)));

  private static Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>> ValleyFreedom(
    NodeLabelledDigraph<string, int> digraph, Zen<BigInteger> time,
    Zen<BigInteger> pathLength)
  {
    // At all times, either a node has no route, or it has a route with the default LP and weight.
    var safety = Lang.Globally(RouteEnvironment.IfValue(r => Zen.And(DefaultLpWeightPath(r),
      r.GetAsPathLength() >= pathLength)));
    // All nodes eventually have routes no worse than their best path length and with no drop communities.
    var eventually =
      Lang.Finally<RouteEnvironment>(time,
        r => Zen.And(r.GetResultValue(), r.GetAsPathLength() <= pathLength, r.NoDropCommunities(digraph)));
    return Lang.Intersect(safety, eventually);
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> ValleyFreedom(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    // infer the destination as the last edge node
    var destination = LastEdgeNode(digraph);
    var destinationPod = digraph.L(destination);

    var initialRoutes = InitialRoutes(digraph, destination);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => r => r.GetResultValue());
    var annotations = digraph.MapNodes(n =>
    {
      var indexTime = n.DistanceFromDestinationEdge(digraph.L(n), destination, destinationPod);
      var witnessTime = symbolicTimes[indexTime].Value;
      // We need the precise best path length here as dist can be arbitrarily larger than the path length,
      // and we need to ensure that nodes never have routes with path length larger than their best.
      var bestPathLength = new BigInteger(indexTime);
      return ValleyFreedom(digraph, witnessTime, bestPathLength);
    });

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional, initialRoutes, annotations, monolithicProperties,
      System.Array.Empty<ISymbolic>(), symbolicTimes);
  }

  public static AnglerFatTreeNetwork<RouteEnvironment> ValleyFreedomAllToR(NodeLabelledDigraph<string, int> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions)
  {
    var destination = new SymbolicDestination(digraph);
    var initialRoutes = InitialRoutes(digraph, destination);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => RouteEnvironmentExtensions.GetResultValue);
    var annotations = digraph.MapNodes(n =>
    {
      var witnessTime = destination.SymbolicDistanceCases(n, digraph.L(n), symbolicTimes.Select(t => t.Value).ToList());
      // We need the precise best path length here as dist can be arbitrarily larger than the path length,
      // and we need to ensure that nodes never have routes with path length larger than their best.
      var bestPathLength = destination.SymbolicDistance(n, digraph.L(n));
      return ValleyFreedom(digraph, witnessTime, bestPathLength);
    });

    return new AnglerFatTreeNetwork<RouteEnvironment>(digraph, transferFunctions,
      RouteEnvironmentExtensions.MinOptional, initialRoutes, annotations, monolithicProperties,
      new[] {destination}, symbolicTimes);
  }

  /// <summary>
  /// Return a predicate over a hijack route that, if it is for the destination prefix, then it must always be internal.
  /// </summary>
  /// <param name="destinationPrefix"></param>
  /// <returns></returns>
  private static Func<Zen<Pair<RouteEnvironment, bool>>, Zen<BigInteger>, Zen<bool>> HijackFilteringSafety(
    Zen<Ipv4Prefix> destinationPrefix) =>
    Lang.Globally<Pair<RouteEnvironment, bool>>(r =>
      Zen.Implies(r.Item1().HasPrefixRoute(destinationPrefix), Zen.Not(r.Item2())));

  public static AnglerFatTreeNetwork<Pair<RouteEnvironment, bool>> FatTreeHijackFiltering(
    NodeLabelledDigraph<string, int> digraph,
    IEnumerable<string> externalPeers,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions,
    IReadOnlyDictionary<string, List<Ipv4Prefix>> prefixes)
  {
    // external neighbors send symbolic routes
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("external-route", externalPeers);
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
        (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, destinationPrefix.Value),
        r => r.Item1());

    // the key property: nodes have routes for the destination prefix, and those routes are internal
    var hasInternalRoute = IsInternal(r => r.HasPrefixRoute(destinationPrefix.Value));
    var monolithicProperties =
      digraph.MapNodes(n => externalRoutes.ContainsKey(n)
        ? Lang.True<Pair<RouteEnvironment, bool>>()
        : hasInternalRoute);
    var modularProperties = digraph.MapNodes(n =>
      externalRoutes.ContainsKey(n)
        ? Lang.Globally(monolithicProperties[n])
        : Lang.Finally(lastTime, monolithicProperties[n]));
    // eventually, every node should have a route to the destination
    // NOTE: since the digraph contains the external peers, this will construct superfluous annotations for them
    // (we won't use those annotations, due to the check below ensuring that external peers' annotations are just G(true))
    var eventuallyAnnotations = FatTreeSymbolicTimes.FinallyAnnotations(digraph,
      destination, hasInternalRoute, symbolicTimes.Select(s => s.Value).ToList());
    var annotations = digraph.MapNodes(n => externalRoutes.ContainsKey(n)
      ? modularProperties[n]
      : Lang.Intersect(HijackFilteringSafety(destinationPrefix.Value), eventuallyAnnotations[n]));

    // collect all the symbolics together
    var symbolics = new ISymbolic[] {destinationPrefix}.Concat(externalRoutes.Values).Concat(symbolicTimes).ToArray();
    return new AnglerFatTreeNetwork<Pair<RouteEnvironment, bool>>(digraph, liftedTransferFunctions, liftedMerge,
      initialRoutes, annotations, modularProperties, monolithicProperties, symbolics);
  }

  public static AnglerFatTreeNetwork<Pair<RouteEnvironment, bool>> FatTreeHijackFilteringAllToR(
    NodeLabelledDigraph<string, int> digraph,
    IEnumerable<string> externalPeers,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions,
    IReadOnlyDictionary<string, List<Ipv4Prefix>> prefixes)
  {
    // external neighbors send symbolic routes
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("external-route", externalPeers);
    var destination = new SymbolicDestination(digraph);
    // if a node in the graph equals the destination, then the destination prefix is one of that node's prefixes
    var destinationPrefix = new SymbolicValue<Ipv4Prefix>("destination-prefix",
      // we need the .Where() here to filter out non-edge nodes; otherwise the constraint will try to look up prefixes[k]
      // for a node k which is not in prefixes!
      p1 => digraph.Nodes.Where(n => n.IsEdge()).ForAll(n =>
        Zen.Implies(destination.EqualsDigraph(digraph, n), prefixes[n].Exists(p2 => p1 == p2))));
    var initialRoutes =
      digraph.MapNodes(n =>
        externalRoutes.TryGetValue(n, out var externalRoute)
          // external neighbors start with routes with the ghost boolean variable set to true
          ? Pair.Create(externalRoute.Value, Zen.True())
          // internal nodes start with routes with the ghost boolean variable set to false
          : Pair.Create(
            Zen.Constant(new RouteEnvironment()).WithPrefix(destinationPrefix.Value)
              .WithResultValue(destination.EqualsDigraph(digraph, n)),
            Zen.False()));
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(5);
    var lastTime = symbolicTimes[^1].Value;

    // lift the original transfer and merge to pairs
    var liftedTransferFunctions = digraph.MapEdges(e => Lang.Product(transferFunctions[e], Lang.Identity<bool>()));
    var liftedMerge =
      Lang.MergeBy<Pair<RouteEnvironment, bool>, RouteEnvironment>(
        (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, destinationPrefix.Value),
        r => r.Item1());

    // the key property: nodes have routes for the destination prefix, and those routes are internal
    var hasInternalRoute = IsInternal(r => r.HasPrefixRoute(destinationPrefix.Value));
    var monolithicProperties =
      digraph.MapNodes(n => externalRoutes.ContainsKey(n)
        ? Lang.True<Pair<RouteEnvironment, bool>>()
        : hasInternalRoute);
    var modularProperties = digraph.MapNodes(n =>
      externalRoutes.ContainsKey(n)
        ? Lang.Globally(monolithicProperties[n])
        : Lang.Finally(lastTime, monolithicProperties[n]));
    // eventually, every node should have a route to the destination
    // NOTE: since the digraph contains the external peers, this will construct superfluous annotations for them
    // (we won't use those annotations, due to the check below ensuring that external peers' annotations are just G(true))
    var eventuallyAnnotations = FatTreeSymbolicTimes.FinallyAnnotations(digraph,
      destination, hasInternalRoute, symbolicTimes.Select(s => s.Value).ToList());
    var annotations = digraph.MapNodes(n => externalRoutes.ContainsKey(n)
      ? modularProperties[n]
      : Lang.Intersect(HijackFilteringSafety(destinationPrefix.Value), eventuallyAnnotations[n]));

    // collect all the symbolics together
    var symbolics = new ISymbolic[] {destinationPrefix, destination}.Concat(externalRoutes.Values).Concat(symbolicTimes)
      .ToArray();
    return new AnglerFatTreeNetwork<Pair<RouteEnvironment, bool>>(digraph, liftedTransferFunctions, liftedMerge,
      initialRoutes, annotations, modularProperties, monolithicProperties, symbolics);
  }

  /// <summary>
  /// Lift a predicate over a route to a predicate over a route and a boolean,
  /// that holds only when the boolean is false.
  /// </summary>
  /// <param name="routePredicate"></param>
  /// <returns></returns>
  private static Func<Zen<Pair<RouteEnvironment, bool>>, Zen<bool>> IsInternal(
    Func<Zen<RouteEnvironment>, Zen<bool>> routePredicate) =>
    Lang.Both<RouteEnvironment, bool>(routePredicate, Zen.Not);
}

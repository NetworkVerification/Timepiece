using System.Numerics;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Queries;

/// <summary>
/// Queries performed by Bagpipe related to the Internet2 network.
/// See the Bagpipe paper for more information.
/// </summary>
public static class Internet2
{
  /// <summary>
  ///   The block to external community tag used by Internet2.
  /// </summary>
  private const string BlockToExternalCommunity = "11537:888";

  /// <summary>
  /// Community tag for identifying low-value peer connections.
  /// </summary>
  private const string LowPeersCommunity = "11537:40";

  /// <summary>
  /// Community tag for identifying lower-than-peer connections.
  /// </summary>
  private const string LowerThanPeersCommunity = "11537:60";

  /// <summary>
  /// Community tag for identifying equal-to-peer-value connections.
  /// </summary>
  private const string EqualToPeersCommunity = "11537:100";

  /// <summary>
  /// Community tag for identifying low-value connections.
  /// </summary>
  private const string LowCommunity = "11537:140";

  /// <summary>
  /// Community tag for identifying high-value peer connections.
  /// </summary>
  private const string HighPeersCommunity = "11537:160";

  /// <summary>
  /// Community tag for identifying high-value connections.
  /// </summary>
  private const string HighCommunity = "11537:260";

  /// <summary>
  /// The nodes of Internet2's AS.
  /// </summary>
  public static readonly string[] Internet2Nodes =
    {"atla-re1", "chic", "clev-re1", "hous", "kans-re1", "losa", "newy-re1", "salt-re1", "seat-re1", "wash"};

  /// <summary>
  ///   Addresses for neighbors in the OTHER-INTERNAL, PAIX and WILC peer group of the internal nodes.
  ///   These connections should also be considered internal.
  /// </summary>
  private static readonly string[] OtherInternalNodes =
  {
    // OTHER-INTERNAL peer group
    "64.57.16.133", "64.57.16.196", "64.57.16.4", "64.57.16.68", "64.57.17.133", "64.57.17.194",
    "64.57.17.7", "64.57.17.71", "64.57.19.2",
    "64.57.28.251", // PAIX group (Palo Alto Internet eXchange)
    "64.57.28.252", // WILC group
  };

  private static readonly IEnumerable<string> InternalNodes = Internet2Nodes.Concat(OtherInternalNodes);

  /// <summary>
  ///   A prefix corresponding to the internal nodes of Internet2.
  /// </summary>
  private static readonly Ipv4Prefix InternalPrefix = new("64.57.28.0", "64.57.28.255");

  /// <summary>
  ///   Predicate that the route is for the internal prefix.
  /// </summary>
  /// <param name="env"></param>
  /// <returns></returns>
  public static Zen<bool> HasInternalRoute(Zen<RouteEnvironment> env)
  {
    return Zen.And(env.GetResultValue(), env.GetPrefix() == InternalPrefix);
  }

  private static Zen<bool> MaxPrefixLengthIs32(Zen<RouteEnvironment> env) =>
    env.GetPrefix().GetPrefixLength() <= new UInt<_6>(32);

  /// <summary>
  ///   Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  public static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env)
  {
    return Zen.Implies(env.GetResultValue(), Zen.Not(env.GetCommunities().Contains(BlockToExternalCommunity)));
  }

  /// <summary>
  /// Prefixes that are considered Martians.
  /// Must not be advertised or accepted.
  /// </summary>
  private static readonly (Ipv4Wildcard, UInt<_6>)[] MartianPrefixes =
  {
    (new Ipv4Wildcard("0.0.0.0", "0.255.255.255"), new UInt<_6>(8)), // local network 0.0.0.0/8
    (new Ipv4Wildcard("127.0.0.0", "0.255.255.255"), new UInt<_6>(8)), // loopback 127.0.0.0/8
    (new Ipv4Wildcard("255.255.255.255", "0.0.0.0"), new UInt<_6>(32)), // limited broadcast
  };

  /// <summary>
  /// Check that a given route is not for a Martian prefix.
  /// </summary>
  /// <param name="env"></param>
  /// <returns></returns>
  private static Zen<bool> NonMartianRoute(Zen<RouteEnvironment> env)
  {
    var matchesAnyMartian = MartianPrefixes.Aggregate(Zen.False(), (b, martian) =>
      Zen.Or(b, Zen.Constant(martian.Item1).MatchesPrefix(env.GetPrefix(), martian.Item2, new UInt<_6>(32))));
    return Zen.Implies(env.GetResultValue(), Zen.Not(matchesAnyMartian));
  }

  /// <summary>
  /// Assign a fresh symbolic variable as an external route from each of the given nodes.
  /// If a constraint is given, apply it to every symbolic variable.
  /// </summary>
  /// <param name="externalPeers"></param>
  /// <param name="constraint"></param>
  /// <returns></returns>
  private static Dictionary<string, SymbolicValue<RouteEnvironment>>
    ExternalRoutes(IEnumerable<string> externalPeers, Func<Zen<RouteEnvironment>, Zen<bool>>? constraint = null) =>
    externalPeers.ToDictionary(e => e, e => constraint is null
      ? new SymbolicValue<RouteEnvironment>($"external-route-{e}")
      : new SymbolicValue<RouteEnvironment>($"external-route-{e}", constraint));

  /// <summary>
  ///   Construct a NetworkQuery with constraints that every external node symbolic does not have the BTE tag,
  ///   and check that all external nodes never have a BTE tag.
  /// </summary>
  /// <param name="externalPeers"></param>
  /// <param name="graph"></param>
  /// <returns></returns>
  public static NetworkQuery<RouteEnvironment, string> BlockToExternal(Digraph<string> graph,
    IEnumerable<string> externalPeers)
  {
    var externalRoutes = ExternalRoutes(externalPeers, BteTagAbsent);
    // external nodes start with a route, internal nodes do not
    var initialRoutes = graph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    var monolithicProperties =
      graph.MapNodes(n =>
        InternalNodes.Contains(n) ? Lang.True<RouteEnvironment>() : BteTagAbsent);
    // annotations and modular properties are the same
    var modularProperties = graph.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().ToArray();
    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolics, monolithicProperties,
      modularProperties, modularProperties);
  }

  public static NetworkQuery<RouteEnvironment, string> NoMartians(Digraph<string> digraph,
    IEnumerable<string> externalPeers)
  {
    var externalRoutes = ExternalRoutes(externalPeers, MaxPrefixLengthIs32);
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    // internal nodes must not select martian routes
    var monolithicProperties = digraph.MapNodes(n =>
      InternalNodes.Contains(n)
        ? Lang.Intersect<RouteEnvironment>(NonMartianRoute, MaxPrefixLengthIs32)
        : Lang.True<RouteEnvironment>());
    // annotations and modular properties are the same
    var modularProperties = digraph.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().ToArray();
    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolics, monolithicProperties,
      modularProperties, modularProperties);
  }

  public static NetworkQuery<RouteEnvironment, string> GaoRexford(Digraph<string> digraph,
    IEnumerable<string> externalPeers)
  {
    // Bagpipe verifies this with a lot of handcrafted analysis:
    // finding the neighbors and then determining which are which
    // could we reuse their findings?
    // see https://github.com/konne88/bagpipe/blob/master/src/bagpipe/racket/test/resources/internet2-properties.rkt
    // var monolithicProperties = digraph.MapNodes(n => InternalNodes.Contains(n) ?
    // Lang.Intersect<RouteEnvironment>(MaxPrefixLengthIs32) : Lang.True<RouteEnvironment>());
    throw new NotImplementedException();
  }

  private static Zen<bool> ExternalValidRouteExists(IEnumerable<Zen<RouteEnvironment>> externalRoutes) =>
    Zen.Or(externalRoutes.Select(e => Zen.And(e.GetResultValue(), NonMartianRoute(e))));

  public static NetworkQuery<RouteEnvironment, string> Reachable(Digraph<string> digraph,
    IEnumerable<string> externalPeers)
  {
    var externalRoutes = ExternalRoutes(externalPeers, r => Zen.And(NonMartianRoute(r), MaxPrefixLengthIs32(r)));
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(2);
    // make the external adjacent time constraint strictly greater than 0
    symbolicTimes[0].Constraint = t => t > BigInteger.Zero;
    var externalAdjacentTime = symbolicTimes[0].Value;
    var internalTime = symbolicTimes[1].Value;
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties = digraph.MapNodes(n =>
      Internet2Nodes.Contains(n)
        // internal nodes: if an external route exists, then we have a route
        ? r => Zen.Implies(ExternalValidRouteExists(externalRoutes.Values.Select(s => s.Value)),
          r.GetResultValue())
        // no check on external nodes
        : Lang.True<RouteEnvironment>());
    var modularProperties = digraph.MapNodes(n =>
      Internet2Nodes.Contains(n)
        // eventually, if an external route exists, internal nodes have a route
        ? Lang.Finally(lastTime, monolithicProperties[n])
        : Lang.Globally(Lang.True<RouteEnvironment>()));
    // FIXME: update acc. to TinyWanSoundAnnotationsPass in BooleanTests.cs
    var annotations = digraph.MapNodes(n =>
      Lang.Intersect(
        Internet2Nodes.Contains(n)
          // internal nodes get routes at 2 different possible times
          // case 1: an adjacent external peer has a route. we get a route once they send it to us
          // case 2: no adjacent external peer has a route. we get a route once an internal neighbor sends it to us
          ? Lang.Finally(
            Zen.If(ExternalNeighborHasRoute(digraph, n, externalRoutes), externalAdjacentTime, internalTime),
            monolithicProperties[n])
          // external nodes get routes at 2 different possible times also
          // case 3: external peer starts with a route.
          // case 4: external peer does not start with a route. it gets a route once it receives it from the network
          : Lang.Globally<RouteEnvironment>(r =>
            Zen.If(
              externalRoutes.TryGetValue(n, out var externalRoute) ? externalRoute.Value.GetResultValue() : Zen.False(),
              r.GetResultValue(), Zen.True())),
        // routes should always have prefix lengths at most 32
        Lang.Globally<RouteEnvironment>(MaxPrefixLengthIs32),
        // routes should not be for a martian prefix
        Lang.Globally<RouteEnvironment>(NonMartianRoute)));
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().Concat(symbolicTimes).ToArray();
    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolics, monolithicProperties, modularProperties,
      annotations);
  }

  /// <summary>
  /// Return a constraint that one of the node's external neighbors has a route.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="node"></param>
  /// <param name="externalRoutes"></param>
  /// <returns></returns>
  private static Zen<bool> ExternalNeighborHasRoute(Digraph<string> digraph, string node,
    IReadOnlyDictionary<string, SymbolicValue<RouteEnvironment>> externalRoutes)
  {
    return digraph[node].Aggregate(Zen.False(),
      (b, neighbor) => externalRoutes.TryGetValue(neighbor, out var externalRoute)
        // if the neighbor is external, add a constraint that it can have a value
        ? Zen.Or(b, externalRoute.Value.GetResultValue())
        // otherwise, we can just skip it
        : b);
  }
}

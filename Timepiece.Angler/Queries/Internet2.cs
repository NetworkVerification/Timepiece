using System.Numerics;
using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Queries;

/// <summary>
///   Queries performed by Bagpipe related to the Internet2 network.
///   See the Bagpipe paper for more information.
/// </summary>
public static class Internet2
{
  /// <summary>
  ///   The block to external community tag used by Internet2.
  /// </summary>
  private const string BlockToExternalCommunity = "11537:888";

  /// <summary>
  ///   Community tag for identifying low-value peer connections.
  /// </summary>
  private const string LowPeersCommunity = "11537:40";

  /// <summary>
  ///   Community tag for identifying lower-than-peer connections.
  /// </summary>
  private const string LowerThanPeersCommunity = "11537:60";

  /// <summary>
  ///   Community tag for identifying equal-to-peer-value connections.
  /// </summary>
  private const string EqualToPeersCommunity = "11537:100";

  /// <summary>
  ///   Community tag for identifying low-value connections.
  /// </summary>
  private const string LowCommunity = "11537:140";

  /// <summary>
  ///   Community tag for identifying high-value peer connections.
  /// </summary>
  private const string HighPeersCommunity = "11537:160";

  /// <summary>
  ///   Community tag for identifying high-value connections.
  /// </summary>
  private const string HighCommunity = "11537:260";

  /// <summary>
  ///   The nodes of Internet2's AS.
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
    "64.57.28.252" // WILC group
  };

  /// <summary>
  ///   Addresses for the AL2S_MGMT peer group.
  ///   See https://internet2.edu/services/layer-2-service/ for what AL2S is.
  /// </summary>
  private static readonly string[] AdvancedLayer2ServiceManagementNodes =
  {
    "64.57.25.164", "64.57.25.165", "64.57.24.204", "64.57.24.205", "64.57.25.124"
  };

  private static readonly IEnumerable<string> InternalNodes = Internet2Nodes.Concat(OtherInternalNodes);

  /// <summary>
  ///   A prefix corresponding to the internal nodes of Internet2.
  /// </summary>
  private static readonly Ipv4Prefix InternalPrefix = new("64.57.28.0", "64.57.28.255");

  /// <summary>
  ///   Prefixes that are considered Martians.
  ///   Must not be advertised or accepted.
  ///   Mostly taken from Internet2's configs: see the SANITY-IN policy's block-martians term.
  /// </summary>
  private static readonly (Ipv4Wildcard, UInt<_6>)[] MartianPrefixes =
  {
    (new Ipv4Wildcard("0.0.0.0", "255.255.255.255"), new UInt<_6>(0)), // default route 0.0.0.0/0
    (new Ipv4Wildcard("10.0.0.0", "0.255.255.255"), new UInt<_6>(8)), // RFC1918 local network 10.0.0.0/8
    (new Ipv4Wildcard("127.0.0.0", "0.255.255.255"), new UInt<_6>(8)), // RFC3330 loopback 127.0.0.0/8
    (new Ipv4Wildcard("169.254.0.0", "0.0.255.255"), new UInt<_6>(16)), // RFC3330 link-local addresses 169.254.0.0/16
    (new Ipv4Wildcard("172.16.0.0", "0.15.255.255"), new UInt<_6>(12)), // RFC1918 private addresses 172.16.0.0/12
    (new Ipv4Wildcard("192.0.2.0", "0.0.0.255"), new UInt<_6>(24)), // IANA reserved 192.0.2.0/24
    (new Ipv4Wildcard("192.88.99.1", "0.0.0.0"), new UInt<_6>(32)), // 6to4 relay 192.88.99.1/32
    (new Ipv4Wildcard("192.168.0.0", "0.0.255.255"), new UInt<_6>(16)), // RFC1918 private addresses 192.168.0.0/16
    (new Ipv4Wildcard("198.18.0.0", "0.1.255.255"),
      new UInt<_6>(15)), // RFC2544 network device benchmarking 198.18.0.0/15
    (new Ipv4Wildcard("224.0.0.0", "15.255.255.255"), new UInt<_6>(4)), // RFC3171 multicast group addresses 224.0.0.0/4
    (new Ipv4Wildcard("240.0.0.0", "15.255.255.255"), new UInt<_6>(4)), // RFC3330 special-use addresses 240.0.0.0/4
    (new Ipv4Wildcard("255.255.255.255", "0.0.0.0"), new UInt<_6>(32)) // limited broadcast -- used?
  };

  // List of prefixes which Abilene originates
  private static readonly (Ipv4Wildcard, UInt<_6>)[] InternalPrefixes =
  {
    // Internet2 Backbone
    (new Ipv4Wildcard("64.57.16.0", "0.0.15.255"), new UInt<_6>(20)),
    // Transit VRF
    (new Ipv4Wildcard("64.57.22.0", "0.0.0.255"), new UInt<_6>(24)),
    (new Ipv4Wildcard("64.57.23.240", "0.0.0.15"), new UInt<_6>(28)),
    // Abilene Backbone
    (new Ipv4Wildcard("198.32.8.0", "0.0.3.255"), new UInt<_6>(22)),
    // Abilene Observatory
    (new Ipv4Wildcard("198.32.12.0", "0.0.3.255"), new UInt<_6>(22)),
    // MANLAN
    (new Ipv4Wildcard("198.32.154.0", "0.0.0.255"), new UInt<_6>(24)),
    (new Ipv4Wildcard("198.71.45.0", "0.0.0.255"), new UInt<_6>(24)),
    (new Ipv4Wildcard("198.71.46.0", "0.0.0.255"), new UInt<_6>(24))
  };

  private static Zen<bool> MaxPrefixLengthIs32(Zen<RouteEnvironment> env)
  {
    return env.GetPrefix().GetPrefixLength() <= new UInt<_6>(32);
  }

  /// <summary>
  ///   Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  public static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env)
  {
    return Zen.Implies(env.GetResultValue(), Zen.Not(env.GetCommunities().Contains(BlockToExternalCommunity)));
  }

  /// <summary>
  ///   Check that if a given route exists, it does not match any of the given prefixes.
  /// </summary>
  /// <param name="prefixes"></param>
  /// <param name="env"></param>
  /// <returns></returns>
  private static Zen<bool> NoPrefixMatch(IEnumerable<(Ipv4Wildcard, UInt<_6>)> prefixes, Zen<RouteEnvironment> env)
  {
    var matchesAnyMartian = prefixes.Aggregate(Zen.False(), (b, martian) =>
      Zen.Or(b, Zen.Constant(martian.Item1).MatchesPrefix(env.GetPrefix(), martian.Item2, new UInt<_6>(32))));
    return Zen.Implies(env.GetResultValue(), Zen.Not(matchesAnyMartian));
  }

  /// <summary>
  ///   Assign a fresh symbolic variable as a route for each of the given <paramref name="nodes"/>.
  ///   Use the given <paramref name="namePrefix"/> to name the route.
  ///   If a <paramref name="constraint"/> is given, apply it to every symbolic variable.
  /// </summary>
  /// <param name="namePrefix">a string prefix for the symbolic variable names</param>
  /// <param name="nodes">the nodes to create symbolic routes for (i.e. the keys to the dictionary)</param>
  /// <param name="constraint">a predicate over <c>RouteEnvironment</c>s</param>
  /// <returns>a dictionary from nodes to symbolic variables</returns>
  private static Dictionary<string, SymbolicValue<RouteEnvironment>> SymbolicRoutes(string namePrefix,
    IEnumerable<string> nodes, Func<Zen<RouteEnvironment>, Zen<bool>>? constraint = null)
  {
    return nodes.ToDictionary(e => e, e => constraint is null
      ? new SymbolicValue<RouteEnvironment>($"{namePrefix}-{e}")
      : new SymbolicValue<RouteEnvironment>($"{namePrefix}-{e}", constraint));
  }

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
    var externalRoutes = SymbolicRoutes("external-route", externalPeers, BteTagAbsent);
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
    var externalRoutes = SymbolicRoutes("external-route", externalPeers, MaxPrefixLengthIs32);
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    // internal nodes must not select martian routes
    var monolithicProperties = digraph.MapNodes(n =>
      InternalNodes.Contains(n)
        ? Lang.Intersect<RouteEnvironment>(env => NoPrefixMatch(MartianPrefixes, env), MaxPrefixLengthIs32)
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

  /// <summary>
  /// Returns true if there exists a route in the given <paramref name="routes"/> that has a value and equals
  /// the given <paramref name="prefix"/>.
  /// </summary>
  /// <param name="prefix"></param>
  /// <param name="routes"></param>
  /// <returns></returns>
  private static Zen<bool> ExistsPrefixRoute(Zen<Ipv4Prefix> prefix, IEnumerable<Zen<RouteEnvironment>> routes) =>
    Zen.Or(routes.Select(e => Zen.And(e.GetResultValue(), e.GetPrefix() == prefix)));

  /// <summary>
  /// Check that all the internal nodes receive a valid route if one is shared by one of them to the others.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="externalPeers"></param>
  /// <returns></returns>
  public static NetworkQuery<RouteEnvironment, string> ReachableInternal(Digraph<string> digraph,
    IEnumerable<string> externalPeers)
  {
    // UMichigan: https://whois.domaintools.com/35.0.0.0
    // var destinationAddress = new Ipv4Prefix("35.0.0.0", "35.255.255.255"); // 35.0.0.0/8
    var destinationAddress = InternalPrefix;
    // var externalRoutes = SymbolicRoutes("external-route", externalPeers,
    // r => Zen.Implies(r.GetResultValue(), r.GetPrefix() == destinationAddress));
    var internalRoutes = SymbolicRoutes("internal-route", Internet2Nodes,
      r => Zen.And(r.GetPrefix() == destinationAddress, r.GetResultValue()));
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(2);
    var initialRoutes = digraph.MapNodes(n =>
      // externalRoutes.TryGetValue(n, out var externalRoute) ? externalRoute.Value :
      internalRoutes.TryGetValue(n, out var internalRoute)
        ? internalRoute.Value
        : new RouteEnvironment {Prefix = destinationAddress});
    var monolithicProperties = digraph.MapNodes(n =>
      Internet2Nodes.Contains(n)
        // internal nodes have a route if one of them has one initially
        ? r => Zen.Implies(
          // if one of the internal routes is true,
          RouteEnvironmentExtensions.ExistsValue(internalRoutes.Values.Select(ir => ir.Value)),
          // then all the internal nodes will have routes
          Zen.And(r.GetResultValue(), r.GetPrefix() == destinationAddress))
        // no check on external nodes
        : Lang.True<RouteEnvironment>());
    var modularProperties = digraph.MapNodes(n =>
      Internet2Nodes.Contains(n)
        ? Lang.Finally(
          // if the node starts with a route, then it gets one at time 0, otherwise at time 1
          Zen.If(internalRoutes[n].Value.GetResultValue(), symbolicTimes[0].Value, symbolicTimes[1].Value),
          monolithicProperties[n])
        : Lang.Globally(monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
      Lang.Intersect(modularProperties[n],
        Lang.Globally<RouteEnvironment>(r => Zen.Implies(r.GetResultValue(),
          r.GetPrefix() == destinationAddress))));
    var symbolics = internalRoutes.Values.Cast<ISymbolic>().Concat(symbolicTimes)
      .ToArray();
    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolics, monolithicProperties, modularProperties,
      annotations);
  }

  /// <summary>
  /// Check that if a valid route comes from the external peers to the network,
  /// then all the internal nodes eventually have that route.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="externalPeers"></param>
  /// <returns></returns>
  public static NetworkQuery<RouteEnvironment, string> Reachable(Digraph<string> digraph,
    IEnumerable<string> externalPeers)
  {
    // the announced external destination prefix
    var destinationPrefix = new SymbolicValue<Ipv4Prefix>("external-prefix", p =>
      Zen.Or(Zen.And(
        // (1) must not be for a martian prefix or an Internet2-internal prefix
        Zen.And(MartianPrefixes.Concat(InternalPrefixes).Select(badPrefix =>
          Zen.Not(Zen.Constant(badPrefix.Item1).MatchesPrefix(p, badPrefix.Item2, new UInt<_6>(32))))),
        // (2) must have a valid prefix length
        p.IsValidPrefixLength())));
    var externalRoutes = SymbolicRoutes("external-route", externalPeers,
      r => Zen.Implies(r.GetResultValue(), Zen.And(r.GetPrefix() == destinationPrefix.Value,
        // no matching terms in AS set
        r.GetAsSet().IsSubsetOf(CSet.Empty<string>()))));
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route)
        ? route.Value
        : Zen.Constant(new RouteEnvironment()).WithPrefix(destinationPrefix.Value));
    // there are 2 symbolic times: when the internal nodes adjacent to the external peer get a route, and when the other internal nodes get a route
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(2);
    // make the external adjacent time constraint strictly greater than 0
    symbolicTimes[0].Constraint = t => t > BigInteger.Zero;
    var nextToPeerTime = symbolicTimes[0].Value;
    var notNextToPeerTime = symbolicTimes[1].Value;
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties = digraph.MapNodes(n =>
      Internet2Nodes.Contains(n)
        // internal nodes: if an external route exists, then we have a route
        ? r => Zen.Implies(ExistsPrefixRoute(destinationPrefix.Value,
            externalRoutes
              // external route exists at a non AL2S_MGMT neighbor
              .Where(ext => !AdvancedLayer2ServiceManagementNodes.Contains(ext.Key))
              .Select(s => s.Value.Value)),
          Zen.And(r.GetResultValue(), r.GetPrefix() == destinationPrefix.Value))
        // no check on external nodes
        : Lang.True<RouteEnvironment>());
    var modularProperties = digraph.MapNodes(n =>
      Internet2Nodes.Contains(n)
        // eventually, if an external route exists, internal nodes have a route
        ? Lang.Finally(lastTime, monolithicProperties[n])
        : Lang.Globally(monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
      Lang.Intersect(
        Internet2Nodes.Contains(n)
          // internal nodes get routes at 2 different possible times
          // case 1: an adjacent external peer has a route. we get a route once they send it to us
          // case 2: no adjacent external peer has a route. we get a route once an internal neighbor sends it to us
          ? Lang.Finally(
            Zen.If(ExternalNeighborHasRoute(digraph, n, externalRoutes), nextToPeerTime, notNextToPeerTime),
            monolithicProperties[n])
          // external nodes get routes at 2 different possible times also
          // case 3: external peer starts with a route.
          // case 4: external peer does not start with a route. it gets a route once it receives it from the network
          // in case 4, we don't care what the peer's route is
          : Lang.Globally<RouteEnvironment>(r =>
            Zen.If(
              externalRoutes.TryGetValue(n, out var externalRoute) ? externalRoute.Value.GetResultValue() : Zen.False(),
              r.GetResultValue(), Zen.True())),
        Lang.Globally<RouteEnvironment>(r =>
          Zen.Implies(r.GetResultValue(),
            Zen.And(r.GetPrefix() == destinationPrefix.Value, r.GetAsSet() == CSet.Empty<string>())))));
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().Concat(symbolicTimes).Append(destinationPrefix).ToArray();
    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolics, monolithicProperties, modularProperties,
      annotations);
  }

  /// <summary>
  ///   Return a constraint that one of the node's external neighbors has a route.
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

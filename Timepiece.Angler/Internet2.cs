using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler;

public static class Internet2
{
  /// <summary>
  ///   The block to external community tag used by Internet2.
  /// </summary>
  private const string BteTag = "11537:888";

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

  public static Zen<bool> MaxPrefixLengthIs32(Zen<RouteEnvironment> env) =>
    env.GetPrefix().GetPrefixLength() <= new UInt<_6>(32);

  /// <summary>
  ///   Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  public static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env)
  {
    return Zen.Implies(env.GetResultValue(), Zen.Not(env.GetCommunities().Contains(BteTag)));
  }

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
  public static Zen<bool> NonMartianRoute(Zen<RouteEnvironment> env)
  {
    var matchesAnyMartian = MartianPrefixes.Aggregate(Zen.False(), (b, martian) =>
      Zen.Or(b, Zen.Constant(martian.Item1).MatchesPrefix(env.GetPrefix(), martian.Item2, new UInt<_6>(32))));
    return Zen.Implies(env.GetResultValue(), Zen.Not(matchesAnyMartian));
  }

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
}

using System.Numerics;
using Timepiece.Angler.UntypedAst;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Datatypes;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler;

public class BlockToExternal : RouteEnvironmentAst
{
  /// <summary>
  /// The block to external tag used by Internet2.
  /// </summary>
  private const string Bte = "11537:888";

  private static readonly string[] InternalNodes =
    {"atla-re1", "chic", "clev-re1", "hous", "kans-re1", "losa", "newy-re1", "salt-re1", "seat-re1", "wash"};

  /// <summary>
  /// Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  private static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env) => Zen.Implies(
    env.GetResult().GetValue(),
    Zen.Not(env.GetCommunities().Contains(Bte)));

  public BlockToExternal(Dictionary<string, NodeProperties> nodes, Ipv4Prefix? destination,
    Dictionary<string, AstPredicate> predicates, Dictionary<string, string?> symbolics, BigInteger? convergeTime) :
    base(nodes, destination, predicates, symbolics, convergeTime)
  {
  }

  /// <summary>
  /// Extract the network from the BlockToExternal class.
  /// </summary>
  /// <param name="f"></param>
  /// <returns></returns>
  public Network<RouteEnvironment, RouteEnvironment> ToNetwork(
    Func<Network<RouteEnvironment, RouteEnvironment>, Network<RouteEnvironment, RouteEnvironment>> f)
  {
    var net = base.ToNetwork();
    return f(net);
  }

  /// <summary>
  /// Add constraints that every external node symbolic does not have the BTE tag,
  /// and check that all external nodes never have a BTE tag.
  /// </summary>
  /// <param name="net"></param>
  /// <returns></returns>
  public static Network<RouteEnvironment, RouteEnvironment> StrongInitialConstraints(
    Network<RouteEnvironment, RouteEnvironment> net)
  {
    foreach (var s in net.Symbolics)
    {
      s.Constraint = BteTagAbsent;
    }

    var modularProperties = net.ModularProperties.Select(p =>
        InternalNodes.Contains(p.Key)
          ? (p.Key, Lang.Globally(Lang.True<RouteEnvironment>()))
          : (p.Key, Lang.Globally<RouteEnvironment>(BteTagAbsent)))
      .ToDictionary(p => p.Item1, p => p.Item2);
    var monolithicProperties =
      net.MonolithicProperties.Select(p =>
          InternalNodes.Contains(p.Key) ? (p.Key, Lang.True<RouteEnvironment>()) : (p.Key, BteTagAbsent))
        .ToDictionary(p => p.Item1, p => p.Item2);
    net.Annotations = modularProperties;
    net.ModularProperties = modularProperties;
    net.MonolithicProperties = monolithicProperties;
    return net;
  }

  public static Network<RouteEnvironment, RouteEnvironment> WeakerInitialConstraints(
    Network<RouteEnvironment, RouteEnvironment> net)
  {
    foreach (var s in net.Symbolics)
    {
      s.Constraint =
        Lang.Intersect<RouteEnvironment>(BteTagAbsent, r => r.GetOriginType() != RouteEnvironment.InternalOrigin);
    }

    // change initial values such that internal nodes may have a route
    var newSymbolics = new List<SymbolicValue<RouteEnvironment>>();
    foreach (var node in InternalNodes)
    {
      var internalRoute = new SymbolicValue<RouteEnvironment>($"internal-route-{node}",
        r =>
          Zen.Implies(r.GetResult().GetValue(), r.GetOriginType() == RouteEnvironment.InternalOrigin));
      newSymbolics.Add(internalRoute);
      // update the initial route of the node
      net.InitialValues[node] = internalRoute.Value;
    }

    // extend the list of symbolics
    net.Symbolics = net.Symbolics.Concat(newSymbolics).ToArray();

    var modularProperties = net.ModularProperties.Select(p =>
        InternalNodes.Contains(p.Key)
          ? (p.Key, Lang.Globally(Lang.True<RouteEnvironment>()))
          : (p.Key, Lang.Globally<RouteEnvironment>(BteTagAbsent)))
      .ToDictionary(p => p.Item1, p => p.Item2);
    var monolithicProperties =
      net.MonolithicProperties.Select(p =>
          InternalNodes.Contains(p.Key) ? (p.Key, Lang.True<RouteEnvironment>()) : (p.Key, BteTagAbsent))
        .ToDictionary(p => p.Item1, p => p.Item2);
    net.Annotations = modularProperties;
    net.ModularProperties = modularProperties;
    net.MonolithicProperties = monolithicProperties;
    return net;
  }
}

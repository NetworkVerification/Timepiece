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

  private readonly string[] _internalNodes =
    {"atla-re1", "chic", "clev-re1", "hous", "kans-re1", "losa", "newy-re1", "salt-re1", "seat-re1", "wash"};

  /// <summary>
  /// Predicate that the BTE tag is not on the route.
  /// </summary>
  private static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env) => Zen.Not(env.GetCommunities().Contains(Bte));

  public BlockToExternal(Dictionary<string, NodeProperties> nodes, Ipv4Prefix? destination,
    Dictionary<string, AstPredicate> predicates, Dictionary<string, string?> symbolics, BigInteger? convergeTime) :
    base(nodes, destination, predicates, symbolics, convergeTime)
  {
  }

  public new Network<RouteEnvironment, RouteEnvironment> ToNetwork()
  {
    var net = base.ToNetwork();
    foreach (var s in net.Symbolics)
    {
      s.Constraint = BteTagAbsent;
    }

    var modularProperties = net.ModularProperties.Select(p =>
        _internalNodes.Contains(p.Key)
          ? (p.Key, Lang.Globally(Lang.True<RouteEnvironment>()))
          : (p.Key, Lang.Globally<RouteEnvironment>(BteTagAbsent)))
      .ToDictionary(p => p.Item1, p => p.Item2);
    var monolithicProperties =
      net.MonolithicProperties.Select(p =>
          _internalNodes.Contains(p.Key) ? (p.Key, Lang.True<RouteEnvironment>()) : (p.Key, BteTagAbsent))
        .ToDictionary(p => p.Item1, p => p.Item2);
    net.Annotations = modularProperties;
    net.ModularProperties = modularProperties;
    net.MonolithicProperties = monolithicProperties;
    return net;
  }
}

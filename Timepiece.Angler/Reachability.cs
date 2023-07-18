using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler;

public static class Reachability
{
  public static AnnotatedNetwork<RouteEnvironment, string, RouteEnvironment> AddReachConstraints(
    AnnotatedNetwork<RouteEnvironment, string, RouteEnvironment> net)
  {
    // change initial values such that internal nodes may have a route
    var newSymbolics = new List<SymbolicValue<RouteEnvironment>>();
    foreach (var node in Internet2.Internet2Nodes)
    {
      var internalRoute = new SymbolicValue<RouteEnvironment>($"internal-route-{node}",
        r =>
          Zen.And(
            // if the node has a route, that route should be:
            // (1) internal, (2) not have the BTE tag set, and (3) be for a valid internal prefix
            Zen.Implies(r.GetResultValue(),
              Zen.And(r.GetOriginType() == RouteEnvironment.InternalOrigin,
                Internet2.BteTagAbsent(r)))
            // TODO: prefix checking isn't implemented so this does nothing
            //r.GetPrefix() == Internet2.InternalPrefix))
            // TODO: is this needed?
            // and it must be that either this node or one of the preceding internal nodes has a route
            // newSymbolics.Aggregate(r.GetResultValue(),
            // (acc, otherInternalRoute) => Zen.Or(acc, otherInternalRoute.Value.GetResultValue()))));
          ));
      newSymbolics.Add(internalRoute);
      // update the initial route of the node
      net.InitialValues[node] = internalRoute.Value;
    }

    // extend the list of symbolics
    net.Symbolics = net.Symbolics.Concat(newSymbolics).ToArray();

    // internal nodes get routes at time 1, external nodes get routes at time 2
    var modularProperties = net.ModularProperties.Select(p =>
        Internet2.Internet2Nodes.Contains(p.Key)
          ? (p.Key, Lang.Finally<RouteEnvironment>(BigInteger.One, Internet2.HasInternalRoute))
          : (p.Key, Lang.Finally<RouteEnvironment>(new BigInteger(2), Internet2.HasInternalRoute)))
      .ToDictionary(p => p.Item1, p => p.Item2);
    var monolithicProperties =
      net.Digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => Internet2.HasInternalRoute);
    net.Annotations = modularProperties;
    net.ModularProperties = modularProperties;
    net.MonolithicProperties = monolithicProperties;

    return net;
  }
}

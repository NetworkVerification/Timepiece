using Timepiece.Networks;

namespace Timepiece.Angler;

public static class BlockToExternal
{
  /// <summary>
  ///   Construct a NetworkQuery with constraints that every external node symbolic does not have the BTE tag,
  ///   and check that all external nodes never have a BTE tag.
  /// </summary>
  /// <param name="externalPeers"></param>
  /// <param name="graph"></param>
  /// <returns></returns>
  public static NetworkQuery<RouteEnvironment, string, RouteEnvironment> StrongInitialConstraints(Digraph<string> graph,
    IEnumerable<string> externalPeers)
  {
    var externalRoutes = externalPeers.ToDictionary(e => e,
      e => new SymbolicValue<RouteEnvironment>($"external-route-{e}", Internet2.BteTagAbsent));

    var monolithicProperties =
      graph.MapNodes(n =>
        Internet2.InternalNodes.Contains(n) ? Lang.True<RouteEnvironment>() : Internet2.BteTagAbsent);
    var modularProperties = graph.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    var initialRoutes = graph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());
    var symbolics = externalRoutes.Values.ToArray();
    return new NetworkQuery<RouteEnvironment, string, RouteEnvironment>(initialRoutes, symbolics, monolithicProperties,
      modularProperties, modularProperties);
  }

  public static AnnotatedNetwork<RouteEnvironment, string, RouteEnvironment> WeakerInitialConstraints(
    AnnotatedNetwork<RouteEnvironment, string, RouteEnvironment> net)
  {
    foreach (var s in net.Symbolics.Where(s => s.Name.StartsWith("external")))
      s.Constraint =
        Internet2.BteTagAbsent;
    // Lang.Intersect<RouteEnvironment>(Internet2.BteTagAbsent,
    // r => r.GetOriginType() != RouteEnvironment.InternalOrigin);
    // change initial values such that internal nodes may have a route
    var newSymbolics = new List<SymbolicValue<RouteEnvironment>>();
    foreach (var node in Internet2.InternalNodes)
    {
      var internalRoute = new SymbolicValue<RouteEnvironment>($"internal-route-{node}");
      // r =>
      // Zen.Implies(r.GetResultValue(), r.GetOriginType() == RouteEnvironment.InternalOrigin));
      newSymbolics.Add(internalRoute);
      // update the initial route of the node
      net.InitialValues[node] = internalRoute.Value;
    }

    // extend the list of symbolics
    net.Symbolics = net.Symbolics.Concat(newSymbolics).ToArray();

    var modularProperties = net.ModularProperties.Select(p =>
        Internet2.InternalNodes.Contains(p.Key)
          ? (p.Key, Lang.Globally(Lang.True<RouteEnvironment>()))
          : (p.Key, Lang.Globally<RouteEnvironment>(Internet2.BteTagAbsent)))
      .ToDictionary(p => p.Item1, p => p.Item2);
    var monolithicProperties =
      net.MonolithicProperties.Select(p =>
          Internet2.InternalNodes.Contains(p.Key)
            ? (p.Key, Lang.True<RouteEnvironment>())
            : (p.Key, Internet2.BteTagAbsent))
        .ToDictionary(p => p.Item1, p => p.Item2);
    net.Annotations = modularProperties;
    net.ModularProperties = modularProperties;
    net.MonolithicProperties = monolithicProperties;

    return net;
  }
}

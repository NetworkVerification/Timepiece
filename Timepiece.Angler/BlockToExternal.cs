using Timepiece.Networks;

namespace Timepiece.Angler;

public static class BlockToExternal
{
  /// <summary>
  /// Add constraints that every external node symbolic does not have the BTE tag,
  /// and check that all external nodes never have a BTE tag.
  /// </summary>
  /// <param name="net"></param>
  /// <returns></returns>
  public static Network<RouteEnvironment, RouteEnvironment> StrongInitialConstraints(
    Network<RouteEnvironment, RouteEnvironment> net)
  {
    // modify the external route symbolics to not have the BTE tag
    foreach (var s in net.Symbolics.Where(s => s.Name.StartsWith("external")))
    {
      s.Constraint = Internet2.BteTagAbsent;
    }

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

  public static Network<RouteEnvironment, RouteEnvironment> WeakerInitialConstraints(
    Network<RouteEnvironment, RouteEnvironment> net)
  {
    foreach (var s in net.Symbolics.Where(s => s.Name.StartsWith("external")))
    {
      s.Constraint =
        Internet2.BteTagAbsent;
      // Lang.Intersect<RouteEnvironment>(Internet2.BteTagAbsent,
      // r => r.GetOriginType() != RouteEnvironment.InternalOrigin);
    }

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

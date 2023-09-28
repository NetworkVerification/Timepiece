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
}

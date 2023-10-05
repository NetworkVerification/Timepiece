using ZenLib;

namespace Timepiece.Angler.Queries;

/// <summary>
/// Queries for checking properties of fat-tree networks.
/// </summary>
public static class FatTree
{
  public static NetworkQuery<RouteEnvironment, string> Reachable(NodeLabelledDigraph<string, int> digraph,
    string destination)
  {
    var initialRoutes =
      digraph.MapNodes(n =>
        n.Equals(destination) ? Zen.Constant(new RouteEnvironment()).WithResultValue(true) : new RouteEnvironment());
    var symbolicTimes = FatTreeSymbolicTimes.AscendingSymbolicTimes(5);
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(_ => RouteEnvironmentExtensions.GetResultValue);
    var modularProperties = digraph.MapNodes(n => Lang.Finally(lastTime, monolithicProperties[n]));
    var annotations = FatTreeSymbolicTimes.FinallyAnnotations<RouteEnvironment>(digraph, destination,
      r => r.GetResultValue(), symbolicTimes.Select(s => s.Value).ToList());

    return new NetworkQuery<RouteEnvironment, string>(initialRoutes, symbolicTimes.Cast<ISymbolic>().ToArray(),
      monolithicProperties, modularProperties, annotations);
  }
}

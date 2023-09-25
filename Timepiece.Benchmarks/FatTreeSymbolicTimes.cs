using System.Numerics;
using ZenLib;

namespace Timepiece.Benchmarks;

public static class FatTreeSymbolicTimes
{
  /// <summary>
  /// Return a list of <c>numTimes</c> many symbolic witness times,
  /// where <c>time[i] &lt; time[i+1]</c> for all <c>i &lt; numTimes</c>.
  /// </summary>
  /// <returns></returns>
  public static IReadOnlyList<SymbolicValue<BigInteger>> AscendingSymbolicTimes(int numTimes)
  {
    var startTime = new SymbolicTime($"tau-0");
    var times = new List<SymbolicTime> {startTime};
    for (var i = 1; i < numTimes; i++)
    {
      // each time needs to be bigger than the last
      var nextTime =
        new SymbolicTime($"tau-{i}", times.Last());
      times.Add(nextTime);
    }

    return times;
  }

  public static Dictionary<string, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>>
    FinallyAnnotations<RouteType>(NodeLabelledDigraph<string, int> g, string destination,
      Func<Zen<RouteType>, Zen<bool>> afterPredicate, IReadOnlyList<SymbolicValue<BigInteger>> symbolicTimes)
  {
    return g.MapNodes(n =>
    {
      var dist = n.DistanceFromDestinationEdge(g.L(n), destination, g.L(destination));
      return Lang.Finally(symbolicTimes[dist].Value, afterPredicate);
    });
  }
}

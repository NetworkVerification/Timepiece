using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ZenLib;

namespace Timepiece;

/// <summary>
///   A symbolic value which represents a particular symbolically-chosen time.
/// </summary>
public record SymbolicTime : SymbolicValue<BigInteger>
{
  public SymbolicTime(string name) : base(name, t => t >= BigInteger.Zero)
  {
  }

  public SymbolicTime(string name, params SymbolicTime[] predecessors) : base(name,
    t => Zen.And(t >= BigInteger.Zero, Zen.And(predecessors.Select(pt => t > pt.Value))))
  {
  }

  /// <summary>
  ///   Return a list of <c>numTimes</c> many symbolic witness times,
  ///   where <c>time[i] &lt; time[i+1]</c> for all <c>i &lt; numTimes</c>.
  /// </summary>
  /// <returns></returns>
  public static IReadOnlyList<SymbolicTime> AscendingSymbolicTimes(int numTimes)
  {
    var startTime = new SymbolicTime("tau-0");
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
}

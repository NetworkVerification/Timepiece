using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Timepiece;

/// <summary>
///   Available statistics to query on modular checks.
/// </summary>
[Flags]
public enum Statistics
{
  None = 0,
  Maximum = 1 << 0,
  Minimum = 1 << 1,
  Average = 1 << 2,
  Median = 1 << 3,
  NinetyNinthPercentile = 1 << 4,
  Total = 1 << 5,
  Individual = 1 << 6,
  Summary = Maximum | Minimum | Average | Median | NinetyNinthPercentile | Total,
  All = Summary | Individual
}

public static class StatisticsExtensions
{
  /// <summary>
  ///   Table field header for wall clock time.
  /// </summary>
  private const string WallTimeHeader = "wall";

  public static string ShortHand(this Statistics stat)
  {
    return stat switch
    {
      Statistics.Maximum => "max",
      Statistics.Minimum => "min",
      Statistics.Average => "avg",
      Statistics.Median => "med",
      Statistics.NinetyNinthPercentile => "99p",
      Statistics.Total => "total",
      Statistics.None | Statistics.Individual | Statistics.Summary | Statistics.All => throw new ArgumentException(
        $"{stat} has no shorthand."),
      _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
    };
  }

  /// <summary>
  ///   Report time statistics for the given dictionary.
  /// </summary>
  /// <param name="times"></param>
  /// <param name="stats"></param>
  /// <param name="wallTime"></param>
  /// <param name="printTable"></param>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public static void ReportTimes<TKey>(IDictionary<TKey, long> times, Statistics stats, long? wallTime,
    bool printTable)
  {
    if (times.Count == 0)
    {
      Console.WriteLine("No time data found!");
      return;
    }

    var headers = new StringBuilder("n");
    var data = new StringBuilder($"{times.Count}");
    foreach (Statistics stat in Enum.GetValues(typeof(Statistics)))
      if ((stats & stat) == stat)
        switch (stat)
        {
          case Statistics.Maximum:
            headers.Append($"\t{stat.ShortHand()}");
            var (maxKey, maxTime) = times.MaxBy(p => p.Value);
            data.Append($"\t{maxTime}");
            Console.WriteLine($"Maximum time: {maxKey} in {maxTime}ms");
            break;
          case Statistics.Minimum:
            headers.Append($"\t{stat.ShortHand()}");
            var (minKey, minTime) = times.MinBy(p => p.Value);
            Console.WriteLine($"Minimum time: {minKey} in {minTime}ms");
            data.Append($"\t{minTime}");
            break;
          case Statistics.Average:
            headers.Append($"\t{stat.ShortHand()}");
            var avg = times.Average(p => p.Value);
            Console.WriteLine($"Average time: {avg}ms");
            data.Append($"\t{avg}");
            break;
          case Statistics.Median:
            headers.Append($"\t{stat.ShortHand()}");
            var midpoint = times.Count / 2;
            var (medianKey, medianTime) = times.OrderBy(p => p.Value).ElementAt(midpoint);
            Console.WriteLine($"Median time: {medianKey} in {medianTime}ms");
            data.Append($"\t{medianTime}");
            break;
          case Statistics.NinetyNinthPercentile:
            headers.Append($"\t{stat.ShortHand()}");
            var ninetyNinth = (int) (times.Count * 0.99);
            var (ninetyNinthKey, ninetyNinthTime) = times.OrderBy(p => p.Value).ElementAt(ninetyNinth);
            Console.WriteLine($"99th percentile time: {ninetyNinthKey} in {ninetyNinthTime}ms");
            data.Append($"\t{ninetyNinthTime}");
            break;
          case Statistics.Total:
            headers.Append($"\t{stat.ShortHand()}");
            var total = times.Sum(p => p.Value);
            Console.WriteLine($"Total time: {total}ms");
            data.Append($"\t{total}");
            break;
          case Statistics.Individual:
            foreach (var (key, time) in times) Console.WriteLine($"{key} took {time}ms");
            break;
          case Statistics.None:
          case Statistics.Summary:
          case Statistics.All:
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(stats));
        }

    // add the wall clock time if it was obtained
    if (wallTime is not null)
    {
      headers.Append($"\t{WallTimeHeader}");
      data.Append($"\t{wallTime}");
    }

    if (!printTable) return;
    Console.WriteLine(headers);
    Console.WriteLine(data);
  }

  public static void ReportCounts<TKey>(IDictionary<TKey, int> counts, Statistics stats)
  {
  }
}

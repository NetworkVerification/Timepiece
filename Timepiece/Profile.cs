using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece;

public static class Profile
{
  /// <summary>
  ///   Table field header for wall clock time.
  /// </summary>
  private const string WallTimeHeader = "wall";

  public static void RunCmpPerNode<T, TV, TS>(AnnotatedNetwork<T, TV, TS> annotatedNetwork)
  {
    RunAnnotatedWithStats(annotatedNetwork);
    RunMonoWithStats(annotatedNetwork);
  }

  public static void RunMonoWithStats<T, TV, TS>(AnnotatedNetwork<T, TV, TS> annotatedNetwork)
  {
    const string headers = "n\ttotal";
    var monoTime = Time(RunMono, annotatedNetwork);
    var data = $"{annotatedNetwork.Topology.Nodes.Length}\t{monoTime}";
    Console.WriteLine($"Monolithic verification took {monoTime}ms");
    Console.WriteLine(headers);
    Console.WriteLine(data);
  }

  public static void RunMono<T, TV, TS>(AnnotatedNetwork<T, TV, TS> annotatedNetwork)
  {
    try
    {
      var s = annotatedNetwork.CheckMonolithic();
      if (!s.HasValue) return;
      s.Value.ReportCheckFailure();
      Console.WriteLine("Error, monolithic verification failed!");
    }
    catch (ZenException e)
    {
      Console.WriteLine("Error, monolithic verification did not complete:");
      Console.WriteLine(e.Message);
    }
  }

  public static void RunAnnotatedWithStats<T, TV, TS>(AnnotatedNetwork<T, TV, TS> annotatedNetwork)
  {
    var processes = Environment.ProcessorCount;
    Console.WriteLine($"Environment.ProcessorCount: {processes}");
    var numNodes = annotatedNetwork.Topology.Nodes.Length;
    var nodeTimes = new ConcurrentDictionary<TV, long>(processes * 2, numNodes);
    long? t = null;
    try
    {
      t = Time(net =>
      {
        var s = net.CheckAnnotationsWith(nodeTimes, LogCheckTime);
        var passed = true;
        var failedNodes = new List<TV>();
        foreach (var (node, counterexample) in s)
        {
          if (!counterexample.HasValue) continue;
          passed = false;
          failedNodes.Add(node);
          Console.WriteLine($"    Counterexample for node {node}:");
          counterexample.Value.ReportCheckFailure();
          Console.WriteLine();
        }

        if (passed)
        {
          Console.WriteLine("    All the modular checks passed!");
          return;
        }

        Console.WriteLine("Error, unsound annotations provided or assertions failed!");
        var allFailed = failedNodes.Aggregate(new StringBuilder(), (builder, n) => builder.Append($" {n}"));
        Console.WriteLine(
          $"Counterexamples occurred at nodes:{allFailed}");
      }, annotatedNetwork);
      Console.WriteLine($"Modular verification took {t}ms");
    }
    catch (ZenException e)
    {
      Console.WriteLine("Error, modular verification did not complete:");
      Console.WriteLine(e.Message);
    }
    finally
    {
      if (!nodeTimes.IsEmpty) ReportCheckTimes(nodeTimes, Statistics.Summary, t, true);
    }
  }

  public static void RunAnnotated<T, TV, TS>(AnnotatedNetwork<T, TV, TS> annotatedNetwork)
  {
    var s = annotatedNetwork.CheckAnnotations();
    if (!s.HasValue)
    {
      Console.WriteLine("    All the modular checks passed!");
      return;
    }

    s.Value.ReportCheckFailure();
    Console.WriteLine("Error, unsound annotations provided or assertions failed!");
  }

  /// <summary>
  ///   Return the milliseconds taken by the given action on the given input.
  /// </summary>
  /// <param name="f"></param>
  /// <param name="input"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static long Time<T>(Action<T> f, T input)
  {
    var timer = Stopwatch.StartNew();
    f(input);
    return timer.ElapsedMilliseconds;
  }

  public static T LogCheckTime<T, TV>(TV node, IDictionary<TV, long> times, Func<T> checkFunction)
  {
    var timer = Stopwatch.StartNew();
    var s = checkFunction();
    // add the time to the dictionary
    times.Add(node, timer.ElapsedMilliseconds);
    return s;
  }

  private static string StatisticShortForm(Statistics stat)
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
  ///   Report the time taken by all the checks.
  /// </summary>
  /// <param name="times"></param>
  /// <param name="stats"></param>
  /// <param name="wallTime"></param>
  /// <param name="printTable"></param>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  private static void ReportCheckTimes<TV>(IDictionary<TV, long> times, Statistics stats, long? wallTime,
    bool printTable)
  {
    var headers = new StringBuilder("n");
    var data = new StringBuilder($"{times.Count}");
    Console.WriteLine("Check statistics:");
    foreach (Statistics stat in Enum.GetValues(typeof(Statistics)))
      if ((stats & stat) == stat)
        switch (stat)
        {
          case Statistics.Maximum:
            headers.Append($"\t{StatisticShortForm(stat)}");
            var (maxNode, maxTime) = times.MaxBy(p => p.Value);
            data.Append($"\t{maxTime}");
            Console.WriteLine($"Maximum check time: node {maxNode} in {maxTime}ms");
            break;
          case Statistics.Minimum:
            headers.Append($"\t{StatisticShortForm(stat)}");
            var (minNode, minTime) = times.MinBy(p => p.Value);
            Console.WriteLine($"Minimum check time: node {minNode} in {minTime}ms");
            data.Append($"\t{minTime}");
            break;
          case Statistics.Average:
            headers.Append($"\t{StatisticShortForm(stat)}");
            var avg = times.Average(p => p.Value);
            Console.WriteLine($"Average check time: {avg}ms");
            data.Append($"\t{avg}");
            break;
          case Statistics.Median:
            headers.Append($"\t{StatisticShortForm(stat)}");
            var midpoint = times.Count / 2;
            var (medianNode, medianTime) = times.OrderBy(p => p.Value).ElementAt(midpoint);
            Console.WriteLine($"Median check time: node {medianNode} in {medianTime}ms");
            data.Append($"\t{medianTime}");
            break;
          case Statistics.NinetyNinthPercentile:
            headers.Append($"\t{StatisticShortForm(stat)}");
            var ninetyNinth = (int) (times.Count * 0.99);
            var (ninetyNinthNode, ninetyNinthTime) = times.OrderBy(p => p.Value).ElementAt(ninetyNinth);
            Console.WriteLine($"99th percentile check time: node {ninetyNinthNode} in {ninetyNinthTime}ms");
            data.Append($"\t{ninetyNinthTime}");
            break;
          case Statistics.Total:
            headers.Append($"\t{StatisticShortForm(stat)}");
            var total = times.Sum(p => p.Value);
            Console.WriteLine($"Total check time: {total}ms");
            data.Append($"\t{total}");
            break;
          case Statistics.Individual:
            foreach (var (node, time) in times) Console.WriteLine($"Node {node} took {time}ms");

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

  /// <summary>
  ///   Available statistics to query on modular checks.
  /// </summary>
  [Flags]
  private enum Statistics
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
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Karesansui.Networks;
using ZenLib;

namespace Karesansui;

public static class Profile
{
  /// <summary>
  /// Run verification for the given network and print the resulting times for comparison
  /// of Minesweeper-style versus Karesansui-style.
  /// </summary>
  /// <param name="network"></param>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TS"></typeparam>
  public static void RunCmp<T, TS>(Network<T, TS> network)
  {
    Console.WriteLine($"Monolithic verification took {Time(RunMono, network)}ms");

    Console.WriteLine($"Modular verification took {Time(RunAnnotated, network)}ms");
  }

  public static void RunCmpPerNode<T, TS>(Network<T, TS> network)
  {
    Console.WriteLine($"Monolithic verification took {Time(RunMono, network)}ms");

    RunAnnotatedWithStats(network);
  }

  public static void RunMono<T, TS>(Network<T, TS> network)
  {
    try
    {
      var s = network.CheckMonolithic();
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

  public static void RunAnnotatedWithStats<T, TS>(Network<T, TS> network)
  {
    var nodeTimes = new Dictionary<string, long>();
    try
    {
      var s = network.CheckAnnotationsWith(nodeTimes, LogCheckTime);
      if (!s.HasValue)
      {
        Console.WriteLine("    All the modular checks passed!");
        ReportCheckTimes(nodeTimes, Statistics.Summary);
        return;
      }
      s.Value.ReportCheckFailure();
      Console.WriteLine("Error, unsound annotations provided or assertions failed!");
    }
    catch (ZenException e)
    {
      Console.WriteLine("Error, modular verification did not complete:");
      Console.WriteLine(e.Message);
    }
  }

  public static void RunAnnotated<T, TS>(Network<T, TS> network)
  {
    var s = network.CheckAnnotations();
    if (!s.HasValue)
    {
      Console.WriteLine("    All the modular checks passed!");
      return;
    }

    s.Value.ReportCheckFailure();
    Console.WriteLine("Error, unsound annotations provided or assertions failed!");
  }

  private static long Time<T, TS>(Action<Network<T, TS>> f, Network<T, TS> network)
  {
    var timer = Stopwatch.StartNew();
    f(network);
    return timer.ElapsedMilliseconds;
  }

  public static Option<State<T, TS>> LogCheckTime<T, TS>(string node,
    Dictionary<string, long> times,
    Func<Option<State<T, TS>>> checkFunction)
  {
    var timer = Stopwatch.StartNew();
    var s = checkFunction();
    // add the time to the dictionary
    times.Add(node, timer.ElapsedMilliseconds);
    return s;
  }

  /// <summary>
  /// Available statistics to query on modular checks.
  /// </summary>
  [Flags]
  private enum Statistics
  {
    None = 0,
    Maximum = 1,
    Minimum = 2,
    Average = 4,
    Total = 8,
    Individual = 16,
    Summary = Maximum | Minimum | Average | Total,
    All = Summary | Individual
  }

  /// <summary>
  /// Report the time taken by all the checks.
  /// </summary>
  /// <param name="times"></param>
  /// <param name="stats"></param>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  private static void ReportCheckTimes(Dictionary<string, long> times, Statistics stats)
  {
    Console.WriteLine("Check statistics:");
    foreach (Statistics stat in Enum.GetValues(typeof(Statistics)))
    {
      if ((stats & stat) == stat)
      {
        switch (stat)
        {
          case Statistics.None:
            break;
          case Statistics.Maximum:
            var (maxNode, maxTime) = times.MaxBy(p => p.Value);
            Console.WriteLine($"Maximum check time: node {maxNode} in {maxTime}ms");
            break;
          case Statistics.Minimum:
            var (minNode, minTime) = times.MinBy(p => p.Value);
            Console.WriteLine($"Maximum check time: node {minNode} in {minTime}ms");
            break;
          case Statistics.Average:
            var avg = times.Average(p => p.Value);
            Console.WriteLine($"Average check time: {avg}ms");
            break;
          case Statistics.Total:
            var total = times.Sum(p => p.Value);
            Console.WriteLine($"Total check time: {total}ms");
            break;
          case Statistics.Individual:
            foreach (var (node, time) in times)
            {
              Console.WriteLine($"Node {node} took {time}ms");
            }
            break;
          case Statistics.Summary:
          case Statistics.All:
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }
  }
}


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
  public static void RunMonoWithStats<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> annotatedNetwork)
  {
    const string headers = "n\ttotal";
    var monoTime = Time(RunMono, annotatedNetwork);
    var data = $"{annotatedNetwork.Digraph.Nodes.Count}\t{monoTime}";
    Console.WriteLine($"Monolithic verification took {monoTime}ms");
    Console.WriteLine(headers);
    Console.WriteLine(data);
  }

  public static void RunMono<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> annotatedNetwork)
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
      Console.WriteLine(e.StackTrace);
    }
  }

  public static void RunAnnotatedWithStats<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> annotatedNetwork)
  {
    var processes = Environment.ProcessorCount;
    Console.WriteLine($"Environment.ProcessorCount: {processes}");
    var numNodes = annotatedNetwork.Digraph.Nodes.Count;
    var nodeTimes = new ConcurrentDictionary<NodeType, long>(processes * 2, numNodes);
    long? t = null;
    try
    {
      t = Time(net =>
      {
        var s = net.CheckAnnotationsWith(nodeTimes, LogCheckTime);
        var passed = true;
        var failedNodes = new List<NodeType>();
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
      if (!nodeTimes.IsEmpty)
      {
        Console.WriteLine("Statistics:");
        StatisticsExtensions.ReportTimes(nodeTimes, Statistics.Summary, t, true);
      }
    }
  }

  public static void RunAnnotated<T, NodeType>(AnnotatedNetwork<T, NodeType> annotatedNetwork)
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

  public static T LogCheckTime<T, NodeType>(NodeType node, IDictionary<NodeType, long> times, Func<T> checkFunction)
  {
    var timer = Stopwatch.StartNew();
    var s = checkFunction();
    // add the time to the dictionary
    times.Add(node, timer.ElapsedMilliseconds);
    return s;
  }
}

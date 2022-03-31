using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    RunAnnotatedWith(network, ReportCheckTime);
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

  public static void RunAnnotatedWith<T, TS>(Network<T, TS> network,
    Func<string, Dictionary<string, long>, Func<Option<State<T, TS>>>, Option<State<T, TS>>> f)
  {
    var nodeTimes = new Dictionary<string, long>();
    try
    {
      var s = network.CheckAnnotationsWith(nodeTimes, f);
      if (!s.HasValue)
      {
        Console.WriteLine("    All the modular checks passed!");
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

  private static Option<State<T, TS>> ReportCheckTime<T, TS>(string node,
    Dictionary<string, long> times,
    Func<Option<State<T, TS>>> checkFunction)
  {
    var timer = Stopwatch.StartNew();
    var s = checkFunction();
    // add the time to the dictionary
    times.Add(node, timer.ElapsedMilliseconds);
    return s;
  }
}

using System;
using System.Diagnostics;
using Karesansui.Networks;

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

  public static void RunMono<T, TS>(Network<T, TS> network)
  {
    var s = network.CheckMonolithic();
    if (!s.HasValue) return;
    s.Value.ReportCheckFailure();
    Console.WriteLine("Error, monolithic verification failed!");
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
}

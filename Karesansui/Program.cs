using System;
using System.Diagnostics;
using Karesansui.Networks;

namespace Karesansui;

// TODOs:
// add a parser to deserialize from JSON
// develop a representation of functions, annotations and predicates
// add code for plugging in different routing algebras and using more complex route types
//  e.g. products/tuples, maps, etc.
public static class Program
{
  /// <summary>
  ///     Main entry point. Runs a simple example.
  /// </summary>
  public static void Main(string[] args)
  {
  }

  /// <summary>
  /// Run verification for the given network and print the resulting times for comparison
  /// of Minesweeper-style versus Karesansui-style.
  /// </summary>
  /// <param name="network"></param>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TS"></typeparam>
  private static void RunCmp<T, TS>(Network<T, TS> network)
  {
    Console.WriteLine($"Monolithic verification took {Time(RunMono, network)}ms");

    Console.WriteLine($"Modular verification took {Time(RunAnnotated, network)}ms");
  }

  private static void RunMono<T, TS>(Network<T, TS> network)
  {
    if (!network.CheckMonolithic()) Console.WriteLine("Error, monolithic verification failed!");
  }

  private static void RunAnnotated<T, TS>(Network<T, TS> network)
  {
    if (network.CheckAnnotations().HasValue)
      Console.WriteLine("Error, unsound annotations provided or assertions failed!");
  }

  private static long Time<T, TS>(Action<Network<T, TS>> f, Network<T, TS> network)
  {
    var timer = Stopwatch.StartNew();
    f(network);
    return timer.ElapsedMilliseconds;
  }
}

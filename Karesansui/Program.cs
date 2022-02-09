using System;
using System.Diagnostics;
using Karesansui.Networks;
using ZenLib;
using Boolean = Karesansui.Networks.Boolean;

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
    if (args.Length == 0)
    {
      Console.WriteLine("No example given. Options are 'sp', 'lp', 'b' or 'ft'.");
      return;
    }

    switch (args[0].ToLower())
    {
      case "b":
        Console.WriteLine("~~ 2-node boolean benchmarks ~~");
        RunCmp(Boolean.Sound());
        break;
      case "sp":
        Console.WriteLine("~~ Simple 3-node shortest path benchmarks ~~");
        RunCmp(Simple.Sound());
        RunCmp(Simple.Unsound());
        break;
      case "lp":
        Console.WriteLine("~~ 2-node local preference benchmarks ~~");
        RunCmp(LocalPref.Sound());
        RunCmp(LocalPref.Unsound());
        break;
      case "tags":
        Console.WriteLine("~~ 3-node bgp tags benchmarks ~~");
        RunCmp(Tags.Sound());
        break;
      case "ft":
        Console.WriteLine("~~ 3-node fault tolerance shortest path benchmarks ~~");
        RunCmp(FaultTolerance<Unit>.Sound());
        break;
      case "sym":
        Console.WriteLine("~~ 3-node symbolic shortest path benchmarks ~~");
        RunCmp(Symbolic.Sound());
        RunCmp(Symbolic.Unsound());
        break;
      case "ap":
        Console.WriteLine("~~ 3-node all-pairs shortest path benchmarks ~~");
        RunCmp(AllPairs.Sound());
        RunCmp(AllPairs.Unsound());
        break;
      case "dis":
        Console.WriteLine("~~ BGP Disagree example ~~");
        RunCmp(Disagree.Sound());
        break;
      default:
        Console.WriteLine(
          "Invalid example given. Options are 'sp', 'lp', 'b', 'ap', 'ft', 'tags', 'sym' or 'dis'.");
        break;
    }
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
    if (!network.CheckAnnotations()) Console.WriteLine("Error, unsound annotations provided or assertions failed!");
  }

  private static long Time<T, TS>(Action<Network<T, TS>> f, Network<T, TS> network)
  {
    var timer = Stopwatch.StartNew();
    f(network);
    return timer.ElapsedMilliseconds;
  }
}

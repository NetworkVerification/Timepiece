using System;
using System.Diagnostics;
using ZenLib;
using WabiSabi.Networks;

namespace WabiSabi;

// TODOs:
// add a parser to deserialize from JSON
// develop a representation of functions, annotations and predicates
// add code for plugging in different routing algebras and using more complex route types
//  e.g. products/tuples, maps, etc.
public class Program
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
                Run(Networks.Boolean.Sound());
                break;
            case "sp":
                Console.WriteLine("~~ Simple 3-node shortest path benchmarks ~~");
                Run(Simple.Sound());
                Run(Simple.Unsound());
                break;
            case "lp":
                Console.WriteLine("~~ 2-node local preference benchmarks ~~");
                Run(Networks.LocalPref.Sound());
                Run(Networks.LocalPref.Unsound());
                break;
            case "tags":
                Console.WriteLine("~~ 3-node bgp tags benchmarks ~~");
                Run(Networks.Tags.Sound());
                break;
            case "ft":
                Console.WriteLine("~~ 3-node fault tolerance shortest path benchmarks ~~");
                Run(Networks.FaultTolerance<Unit>.Sound());
                break;
            case "sym":
                Console.WriteLine("~~ 3-node symbolic shortest path benchmarks ~~");
                Run(Symbolic.Sound());
                Run(Symbolic.Unsound());
                break;
            case "ap":
                Console.WriteLine("~~ 4-node all-pairs shortest path benchmarks ~~");
                Run(Networks.AllPairs.Sound());
                Run(Networks.AllPairs.Unsound());
                break;
            case "dis":
                Console.WriteLine("~~ BGP Disagree example ~~");
                Run(Disagree.Sound());
                break;
            default:
                Console.WriteLine(
                    "Invalid example given. Options are 'sp', 'lp', 'b', 'ap', 'ft', 'tags', 'sym' or 'dis'.");
                break;
        }
    }

    private static void Run<T, TS>(Network<T, TS> network)
    {
        var timer = Stopwatch.StartNew();

        if (!network.CheckMonolithic()) Console.WriteLine("Error, monolithic verification failed!");

        Console.WriteLine($"Monolithic verification took {timer.ElapsedMilliseconds}ms");
        timer.Restart();

        if (!network.CheckAnnotations()) Console.WriteLine("Error, unsound annotations provided or assertions failed!");

        Console.WriteLine($"Modular verification took {timer.ElapsedMilliseconds}ms");
    }
}
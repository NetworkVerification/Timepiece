using System;
using System.Diagnostics;

namespace ZenDemo;

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
        Console.WriteLine("~~ 2-node boolean benchmarks ~~");
        Run(BoolNet.Sound());
        Console.WriteLine("~~ Simple 3-node shortest path benchmarks ~~");
        Run(Simple.Sound());
        Run(Simple.Unsound());
        Console.WriteLine("~~ 2-node local preference benchmarks ~~");
        Run(LocalPref.Sound());
        Run(LocalPref.Unsound());
        Console.WriteLine("~~ 3-node symbolic shortest path benchmarks ~~");
        Run(Symbolic.Sound());
        Run(Symbolic.Unsound());
        Console.WriteLine("~~ 4-node all-pairs shortest path benchmarks ~~");
        Run(AllPairs.Sound());
        // Run(AllPairs.Unsound());
    }

    private static void Run<T>(Network<T> network)
    {
        var timer = Stopwatch.StartNew();

        if (!network.CheckMonolithic()) Console.WriteLine("Error, monolithic verification failed!");

        Console.WriteLine($"Monolithic verification took {timer.ElapsedMilliseconds}ms");
        timer.Restart();

        if (!network.CheckAnnotations()) Console.WriteLine("Error, unsound annotations provided or assertions failed!");

        Console.WriteLine($"Modular verification took {timer.ElapsedMilliseconds}ms");
    }
}
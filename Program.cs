using System;

namespace ZenDemo
{
    // TODOs:
    // add a parser to deserialize from JSON
    // develop a representation of functions, annotations and predicates
    // add code for plugging in different routing algebras and using more complex route types
    //  e.g. products/tuples, maps, etc.
    public class Program
    {
        /// <summary>
        /// Main entry point. Runs a simple example.
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine($"Simple 3-node shortest path benchmarks:");
            Run(Simple.Sound());
            Run(Simple.Unsound());
            Console.WriteLine($"2-node local preference benchmarks:");
            Run(LocalPref.Sound());
            Run(LocalPref.Unsound());
        }

        private static void Run<T>(Network<T> network)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            if (!network.CheckMonolithic())
            {
                Console.WriteLine($"Error, monolithic verification failed!");
            }

            Console.WriteLine($"Monolithic verification took {timer.ElapsedMilliseconds}ms");
            timer.Restart();

            if (!network.CheckAnnotations())
            {
                Console.WriteLine($"Error, unsound annotations provided or assertions failed!");
            }

            Console.WriteLine($"Modular verification took {timer.ElapsedMilliseconds}ms");
        }
    }
}
using System;

namespace ZenDemo
{
    public class Program
    {
        /// <summary>
        /// Main entry point. Runs a simple example.
        /// </summary>
        public static void Main(string[] args)
        {

            var network = Simple.SimpleNetwork();

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

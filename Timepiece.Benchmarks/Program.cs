// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using Timepiece.Benchmarks;

var rootCommand = new RootCommand("Timepiece benchmark runner");
var sizeOption = new Option<uint>(
  new[] {"--size", "-k"},
  description: "The size of the benchmark (number of pods for fattrees)",
  parseArgument: result =>
  {
    if (int.TryParse(result.Tokens.Single().Value, out var size))
    {
      if (size >= 0) return (uint) size;
    }

    result.ErrorMessage = "Size must be a non-negative integer.";
    return 0;
  })
{
  IsRequired = true
};
var destOption = new Option<string>(
  new[] {"--dest", "-d"},
  description: "The destination node of the benchmark")
{
  IsRequired = false
};
var monoOption = new Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically simulating Minesweeper");
var benchArgument = new Argument<BenchmarkType>(
  name: "benchmark",
  description: "The type of benchmark to test (accepts short-hands: 'r', 'l', 'v', 'h'...)",
  parse: result => result.Tokens.Single().Value.Parse());
rootCommand.Add(sizeOption);
rootCommand.Add(destOption);
rootCommand.Add(benchArgument);
rootCommand.Add(monoOption);

rootCommand.SetHandler(
  (uint size, string dest, BenchmarkType bench, bool mono) =>
  {
    Console.WriteLine($"k={size}");
    new Benchmark(size, dest, bench, mono).Run();
  }, sizeOption, destOption, benchArgument, monoOption);

await rootCommand.InvokeAsync(args);

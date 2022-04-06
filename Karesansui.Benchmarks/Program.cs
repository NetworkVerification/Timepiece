// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using Karesansui.Benchmarks;

var rootCommand = new RootCommand("Karesansui benchmark runner");
var sizeOption = new Option<uint>(
  new[] {"--size", "-k"},
  description: "The size of the benchmark (number of pods for fattrees)",
  parseArgument: result => {
    if (int.TryParse(result.Tokens.Single().Value, out var size)) {
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
  IsRequired = true
};
var monoOption = new Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically simulating Minesweeper");
var benchArgument = new Argument<BenchmarkType>(
  "The type of benchmark to test",
  parse: result => result.Tokens.Single().Value.Parse());
rootCommand.Add(sizeOption);
rootCommand.Add(destOption);
rootCommand.Add(benchArgument);
rootCommand.Add(monoOption);

rootCommand.SetHandler((uint size, string dest, BenchmarkType bench, bool mono) =>
  new Benchmark(size, dest, bench, -1, mono).Run(), sizeOption, destOption, benchArgument, monoOption);

await rootCommand.InvokeAsync(args);

// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using Karesansui.Benchmarks;

var rootCommand = new RootCommand("Karesansui benchmark runner");
var sizeOption = new Option<int>(
  new[] {"--size", "-k"},
  description: "The size of the benchmark (number of pods for fattrees)")
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

rootCommand.SetHandler((int size, string dest, BenchmarkType bench, bool mono) =>
  new Benchmark((uint) size, dest, bench, -1, mono).Run(), sizeOption, destOption, benchArgument, monoOption);

await rootCommand.InvokeAsync(args);

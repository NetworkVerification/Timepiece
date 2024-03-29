﻿// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using Timepiece.Benchmarks;
using ZenLib;

ZenSettings.UseLargeStack = true;
ZenSettings.LargeStackSize = 30_000_000;

var rootCommand = new RootCommand("Timepiece benchmark runner");
var sizeOption = new System.CommandLine.Option<uint>(
  new[] {"--size", "-k"},
  description: "The size of the benchmark (number of pods for fattrees)",
  parseArgument: result =>
  {
    if (int.TryParse(result.Tokens.Single().Value, out var size))
      if (size >= 0)
        return (uint) size;

    result.ErrorMessage = "Size must be a non-negative integer.";
    return 0;
  })
{
  IsRequired = true
};
var destOption = new System.CommandLine.Option<string>(
  new[] {"--dest", "-d"},
  "The destination node of the benchmark");
var inferOption = new System.CommandLine.Option<bool>(
  new[] {"--infer", "-I"},
  "If given, infer witness times rather than giving them");
var verboseOption = new System.CommandLine.Option<bool>(
  new[] {"--verbose", "-v"},
  "If given, print Zen formulas to stdout");
var monoOption = new System.CommandLine.Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically simulating Minesweeper");
var benchArgument = new Argument<BenchmarkType>(
  "benchmark",
  description: "The type of benchmark to test (accepts short-hands: 'r', 'l', 'v', 'h'...)",
  parse: result => result.Tokens.Single().Value.Parse());
rootCommand.Add(sizeOption);
rootCommand.Add(destOption);
rootCommand.Add(benchArgument);
rootCommand.Add(verboseOption);
rootCommand.Add(monoOption);
rootCommand.Add(inferOption);

rootCommand.SetHandler(
  (size, dest, bench, verbose, mono, infer) =>
  {
    Console.WriteLine($"k={size}");
    new Benchmark(size, dest, bench, verbose, mono, infer).Run();
  }, sizeOption, destOption, benchArgument, verboseOption, monoOption, inferOption);

await rootCommand.InvokeAsync(args);

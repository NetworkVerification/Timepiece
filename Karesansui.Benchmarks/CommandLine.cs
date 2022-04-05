using Mono.Options;

namespace Karesansui.Benchmarks;

public static class CommandLine
{
  public static Benchmark? Parse(IEnumerable<string> args)
  {
    uint? size = null;
    string? destination = null;
    BenchmarkType? benchmark = null;
    var timeout = -1;

    var p = new OptionSet
    {
      {"k|size=", "The {SIZE} of the benchmark", (uint k) => size = k},
      {"d|dest=", "The {DEST} node of the benchmark", n => destination = n},
      {"b|bench=", "the {BENCHMARK} to test", s => benchmark = s.Parse()},
      {"t|timeout=", "the maximum {TIME} a benchmark should run for before it is cancelled", (int s) => timeout = s}
    };
    try
    {
      var _ = p.Parse(args);
    }
    catch (OptionException e)
    {
      Console.WriteLine(e.Message);
      return null;
    }

    if (size is not null && destination is not null && benchmark is not null)
      return new Benchmark((uint) size, destination, (BenchmarkType) benchmark, timeout);

    p.WriteOptionDescriptions(Console.Out);
    return null;

  }
}

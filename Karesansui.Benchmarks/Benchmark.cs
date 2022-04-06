using Karesansui.Networks;

namespace Karesansui.Benchmarks;

public class Benchmark
{
  public Benchmark(uint n, string destination, BenchmarkType type, int timeout, bool runMonolithic)
  {
    N = n;
    Destination = destination;
    Bench = type;
    Timeout = timeout;
    RunMonolithic = runMonolithic;
  }

  public void Run()
  {
    switch (Bench)
    {
      case BenchmarkType.SpReachable:
        RunProfiler(Sp.Reachability(N, Destination), RunMonolithic);
        break;
      case BenchmarkType.SpPathLength:
        RunProfiler(Sp.PathLength(N, Destination), RunMonolithic);
        break;
      case BenchmarkType.ValleyFree:
        RunProfiler(Vf.ValleyFreeReachable(N, Destination), RunMonolithic);
        break;
      case BenchmarkType.FatTreeHijack:
        RunProfiler(Hijack.HijackFiltered(N, Destination), RunMonolithic);
        break;
      default:
        throw new ArgumentOutOfRangeException(null, Bench, "Invalid argument is not a benchmark type");
    }
  }

  private static void RunProfiler<T, TS>(Network<T, TS> net, bool asMonolithic)
  {
    if (asMonolithic)
    {
      Profile.RunMonoWithStats(net);
    }
    else
    {
      Profile.RunAnnotatedWithStats(net);
    }
  }

  public BenchmarkType Bench { get; set; }

  public string Destination { get; set; }

  public uint N { get; set; }

  public int Timeout { get; set; }

  public bool RunMonolithic { get; set; }
}

public enum BenchmarkType
{
  SpReachable,
  SpPathLength,
  ValleyFree,
  FatTreeHijack,
}

public static class BenchmarkTypeExtensions
{
  public static BenchmarkType Parse(this string s)
  {
    return s switch
    {
      "r" or "reach" => BenchmarkType.SpReachable,
      "l" or "length" => BenchmarkType.SpPathLength,
      "v" or "valley" => BenchmarkType.ValleyFree,
      "h" or "hijack" => BenchmarkType.FatTreeHijack,
      _ => throw new ArgumentException($"{s} does not correspond to a valid BenchmarkType.")
    };
  }
}

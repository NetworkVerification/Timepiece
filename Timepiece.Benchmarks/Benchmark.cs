using Timepiece.Networks;

namespace Timepiece.Benchmarks;

public class Benchmark
{
  public Benchmark(uint n, string? destination, BenchmarkType type, bool runMonolithic)
  {
    N = n;
    if (type == BenchmarkType.ApReachable)
    {
      Destination = "[symbolic]";
    }
    else if (destination is null)
    {
      var edgeNode = FatTree.FatTreeLayer.Edge.Node((uint) (Math.Pow(n, 2) * 1.25 - 1));
      Console.WriteLine($"Inferred destination node: {edgeNode}");
      Destination = edgeNode;
    }
    else
    {
      Destination = destination;
    }

    Bench = type;
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
      case BenchmarkType.SpPathLengthWeak:
        RunProfiler(Sp.PathLengthNoSafety(N, Destination), RunMonolithic);
        break;
      case BenchmarkType.ApReachable:
        RunProfiler(Sp.AllPairsReachability(N), RunMonolithic);
        break;
      case BenchmarkType.ValleyFree:
        RunProfiler(Vf.ValleyFreeReachable(N, Destination), RunMonolithic);
        break;
      case BenchmarkType.ValleyFreeLength:
        RunProfiler(Vf.ValleyFreePathLength(N, Destination), RunMonolithic);
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

  public bool RunMonolithic { get; set; }
}

public enum BenchmarkType
{
  SpReachable,
  SpPathLength,
  SpPathLengthWeak,
  ApReachable,
  ValleyFree,
  ValleyFreeLength,
  FatTreeHijack,
}

public static class BenchmarkTypeExtensions
{
  public static BenchmarkType Parse(this string s)
  {
    return s switch
    {
      "r" or "reach" or "SpReachable" => BenchmarkType.SpReachable,
      "l" or "length" or "SpPathLength" => BenchmarkType.SpPathLength,
      "lw" or "lengthWeak" or "SpPathLengthWeak" => BenchmarkType.SpPathLengthWeak,
      "a" or "allReach" or "ApReachable" => BenchmarkType.ApReachable,
      "v" or "valley" or "ValleyFree" => BenchmarkType.ValleyFree,
      "vl" or "valleyLength" or "ValleyFreeLength" => BenchmarkType.ValleyFreeLength,
      "h" or "hijack" or "FatTreeHijack" => BenchmarkType.FatTreeHijack,
      _ => throw new ArgumentException($"{s} does not correspond to a valid BenchmarkType. Acceptable values:\n" +
                                       "- 'r'/'reach'/'SpReachable' for SpReachable\n" +
                                       "- 'l'/'length'/'SpPathLength' for SpPathLength\n" +
                                       "- 'lw'/'lengthWeak'/'SpPathLengthWeak' for SpPathLengthWeak\n" +
                                       "- 'a'/'allReach'/'ApReachable' for ApReachable\n" +
                                       "- 'v'/'valley'/'ValleyFree' for ValleyFree\n" +
                                       "- 'vl'/'valleyLength'/'ValleyFreeLength' for ValleyFreeLength\n" +
                                       "- 'h'/'hijack'/'FatTreeHijack' for FatTreeHijack")
    };
  }
}

using Timepiece.Networks;

namespace Timepiece.Benchmarks;

public class Benchmark
{
  public Benchmark(uint n, string? destination, BenchmarkType type, bool verbose, bool runMonolithic, bool inferTimes)
  {
    N = n;
    if (type.HasSymbolicDestination())
    {
      // NOTE: this should not match any real node names!
      Destination = "~~[symbolic]~~";
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
    Verbose = verbose;
    RunMonolithic = runMonolithic;
    InferTimes = inferTimes;
  }

  public BenchmarkType Bench { get; set; }

  public string Destination { get; set; }

  public uint N { get; set; }

  public bool RunMonolithic { get; set; }

  public bool Verbose { get; set; }

  public bool InferTimes { get; set; }

  public void Run()
  {
    switch (Bench)
    {
      case BenchmarkType.SpReachable:
        RunProfiler(Sp.Reachability(N, Destination, InferTimes));
        break;
      case BenchmarkType.SpPathLength:
        RunProfiler(Sp.PathLength(N, Destination));
        break;
      case BenchmarkType.SpPathLengthWeak:
        RunProfiler(Sp.PathLengthNoSafety(N, Destination, InferTimes));
        break;
      case BenchmarkType.ApReachable:
        RunProfiler(Sp.AllPairsReachability(N));
        break;
      case BenchmarkType.ApPathLength:
        RunProfiler(Sp.AllPairsPathLength(N));
        break;
      case BenchmarkType.ApPathLengthWeak:
        RunProfiler(Sp.AllPairsPathLengthNoSafety(N));
        break;
      case BenchmarkType.ValleyFree:
        RunProfiler(Vf.ValleyFreeReachable(N, Destination, InferTimes));
        break;
      case BenchmarkType.ValleyFreeLength:
        RunProfiler(Vf.ValleyFreePathLength(N, Destination));
        break;
      case BenchmarkType.ApValleyFree:
        RunProfiler(Vf.AllPairsValleyFreeReachable(N));
        break;
      case BenchmarkType.FatTreeHijack:
        RunProfiler(Hijack.HijackFiltered(N, Destination));
        break;
      case BenchmarkType.ApFatTreeHijack:
        RunProfiler(Hijack.AllPairsHijackFiltered(N));
        break;
      default:
        throw new ArgumentOutOfRangeException(null, Bench, "Invalid argument is not a benchmark type");
    }
  }

  private void RunProfiler<T, TS>(AnnotatedNetwork<T, TS> net)
  {
    net.PrintFormulas = Verbose;
    if (RunMonolithic)
      Profile.RunMonoWithStats(net);
    else
      Profile.RunAnnotatedWithStats(net);
  }
}

public enum BenchmarkType
{
  SpReachable,
  SpPathLength,
  SpPathLengthWeak,
  ApReachable,
  ApPathLength,
  ApPathLengthWeak,
  ValleyFree,
  ValleyFreeLength,
  ApValleyFree,
  FatTreeHijack,
  ApFatTreeHijack
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
      "ar" or "allReach" or "ApReachable" => BenchmarkType.ApReachable,
      "al" or "allLength" or "ApPathLength" => BenchmarkType.ApPathLength,
      "alw" or "AllLengthWeak" or "ApPathLengthWeak" => BenchmarkType.ApPathLengthWeak,
      "v" or "valley" or "ValleyFree" => BenchmarkType.ValleyFree,
      "vl" or "valleyLength" or "ValleyFreeLength" => BenchmarkType.ValleyFreeLength,
      "av" or "allValley" or "ApValleyFree" => BenchmarkType.ApValleyFree,
      "h" or "hijack" or "FatTreeHijack" => BenchmarkType.FatTreeHijack,
      "ah" or "allHijack" or "ApFatTreeHijack" => BenchmarkType.ApFatTreeHijack,
      _ => throw new ArgumentException($"{s} does not correspond to a valid BenchmarkType. Acceptable values:\n" +
                                       "- 'r'/'reach'/'SpReachable' for SpReachable\n" +
                                       "- 'l'/'length'/'SpPathLength' for SpPathLength\n" +
                                       "- 'lw'/'lengthWeak'/'SpPathLengthWeak' for SpPathLengthWeak\n" +
                                       "- 'ar'/'allReach'/'ApReachable' for ApReachable\n" +
                                       "- 'al'/'allLength'/'ApPathLength' for ApPathLength\n" +
                                       "- 'alw'/'allLengthWeak'/'ApPathLengthWeak' for ApPathLengthWeak\n" +
                                       "- 'v'/'valley'/'ValleyFree' for ValleyFree\n" +
                                       "- 'vl'/'valleyLength'/'ValleyFreeLength' for ValleyFreeLength\n" +
                                       "- 'av'/'allValley'/'ApValleyFree' for ApValleyFree\n" +
                                       "- 'h'/'hijack'/'FatTreeHijack' for FatTreeHijack" +
                                       "- 'ah'/'allHijack'/'ApFatTreeHijack' for ApFatTreeHijack\n")
    };
  }

  public static bool HasSymbolicDestination(this BenchmarkType t)
  {
    return t switch
    {
      BenchmarkType.SpReachable => false,
      BenchmarkType.SpPathLength => false,
      BenchmarkType.SpPathLengthWeak => false,
      BenchmarkType.ValleyFree => false,
      BenchmarkType.ValleyFreeLength => false,
      BenchmarkType.FatTreeHijack => false,
      BenchmarkType.ApReachable => true,
      BenchmarkType.ApPathLength => true,
      BenchmarkType.ApPathLengthWeak => true,
      BenchmarkType.ApValleyFree => true,
      BenchmarkType.ApFatTreeHijack => true,
      _ => throw new ArgumentOutOfRangeException(nameof(t), t, null)
    };
  }
}

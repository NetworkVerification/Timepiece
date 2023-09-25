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
      case BenchmarkType.SpReachableSymbolic:
        RunProfiler(Sp.ReachabilitySymbolicTimes(N, Destination));
        break;
      case BenchmarkType.SpPathLength:
        RunProfiler(Sp.PathLength(N, Destination));
        break;
      case BenchmarkType.SpPathLengthWeak:
        RunProfiler(Sp.PathLengthNoSafety(N, Destination, InferTimes));
        break;
      case BenchmarkType.SpPathLengthWeakSymbolic:
        RunProfiler(Sp.PathLengthNoSafetySymbolicTimes(N, Destination));
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
      case BenchmarkType.ValleyFreeSymbolic:
        RunProfiler(Vf.ValleyFreeReachableSymbolicTimes(N, Destination));
        break;
      case BenchmarkType.ApValleyFree:
        RunProfiler(Vf.AllPairsValleyFreeReachable(N));
        break;
      case BenchmarkType.FatTreeHijack:
        RunProfiler(Hijack.HijackFiltered(N, Destination, InferTimes));
        break;
      case BenchmarkType.ApFatTreeHijack:
        RunProfiler(Hijack.AllPairsHijackFiltered(N));
        break;
      default:
        throw new ArgumentOutOfRangeException(null, Bench, "Invalid argument is not a benchmark type");
    }
  }

  private void RunProfiler<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
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
  SpReachableSymbolic,
  SpPathLength,
  SpPathLengthWeak,
  SpPathLengthWeakSymbolic,
  ApReachable,
  ApPathLength,
  ApPathLengthWeak,
  ValleyFree,
  ValleyFreeLength,
  ValleyFreeSymbolic,
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
      "rs" or "reachSymbolic" or "SpReachableSymbolic" => BenchmarkType.SpReachableSymbolic,
      "l" or "length" or "SpPathLength" => BenchmarkType.SpPathLength,
      "lw" or "lengthWeak" or "SpPathLengthWeak" => BenchmarkType.SpPathLengthWeak,
      "lws" or "lengthWeakSymbolic" or "SpPathLengthWeakSymbolic" => BenchmarkType.SpPathLengthWeakSymbolic,
      "ar" or "allReach" or "ApReachable" => BenchmarkType.ApReachable,
      "al" or "allLength" or "ApPathLength" => BenchmarkType.ApPathLength,
      "alw" or "allLengthWeak" or "ApPathLengthWeak" => BenchmarkType.ApPathLengthWeak,
      "v" or "valley" or "ValleyFree" => BenchmarkType.ValleyFree,
      "vl" or "valleyLength" or "ValleyFreeLength" => BenchmarkType.ValleyFreeLength,
      "vs" or "valleySymbolic" or "ValleyFreeSymbolic" => BenchmarkType.ValleyFreeSymbolic,
      "av" or "allValley" or "ApValleyFree" => BenchmarkType.ApValleyFree,
      "h" or "hijack" or "FatTreeHijack" => BenchmarkType.FatTreeHijack,
      "ah" or "allHijack" or "ApFatTreeHijack" => BenchmarkType.ApFatTreeHijack,
      _ => throw new ArgumentException($"{s} does not correspond to a valid BenchmarkType. Acceptable values:\n" +
                                       "- 'r'/'reach'/'SpReachable' for SpReachable\n" +
                                       "- 'rs'/'reachSymbolic'/'SpReachableSymbolic' for SpReachableSymbolic\n" +
                                       "- 'l'/'length'/'SpPathLength' for SpPathLength\n" +
                                       "- 'lw'/'lengthWeak'/'SpPathLengthWeak' for SpPathLengthWeak\n" +
                                       "- 'lws'/'lengthWeakSymbolic'/'SpPathLengthWeakSymbolic' for SpPathLengthWeakSymbolic\n" +
                                       "- 'ar'/'allReach'/'ApReachable' for ApReachable\n" +
                                       "- 'al'/'allLength'/'ApPathLength' for ApPathLength\n" +
                                       "- 'alw'/'allLengthWeak'/'ApPathLengthWeak' for ApPathLengthWeak\n" +
                                       "- 'v'/'valley'/'ValleyFree' for ValleyFree\n" +
                                       "- 'vl'/'valleyLength'/'ValleyFreeLength' for ValleyFreeLength\n" +
                                       "- 'vs'/'valleySymbolic'/'ValleyFreeSymbolic' for ValleyFreeSymbolic\n" +
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
      BenchmarkType.SpReachableSymbolic => false,
      BenchmarkType.SpPathLength => false,
      BenchmarkType.SpPathLengthWeak => false,
      BenchmarkType.SpPathLengthWeakSymbolic => false,
      BenchmarkType.ValleyFree => false,
      BenchmarkType.ValleyFreeLength => false,
      BenchmarkType.ValleyFreeSymbolic => false,
      BenchmarkType.FatTreeHijack => false,
      BenchmarkType.ApReachable => true,
      BenchmarkType.ApPathLength => true,
      BenchmarkType.ApPathLengthWeak => true,
      BenchmarkType.ApValleyFree => true,
      BenchmarkType.ApFatTreeHijack => true,
      _ => throw new ArgumentOutOfRangeException(nameof(t), t,
        "Unable to determine if BenchmarkType is for a symbolic destination.")
    };
  }
}

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
        RunProfiler(Sp.PathLength(N, Destination, InferTimes));
        break;
      case BenchmarkType.SpPathLengthSymbolic:
        RunProfiler(Sp.PathLengthSymbolicTimes(N, Destination));
        break;
      case BenchmarkType.ApReachable:
        RunProfiler(Sp.AllPairsReachability(N));
        break;
      case BenchmarkType.ApReachableSymbolic:
        RunProfiler(Sp.AllPairsReachabilitySymbolicTimes(N));
        break;
      case BenchmarkType.ApPathLength:
        RunProfiler(Sp.AllPairsPathLength(N));
        break;
      case BenchmarkType.ApPathLengthSymbolic:
        RunProfiler(Sp.AllPairsPathLengthSymbolicTimes(N));
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
      case BenchmarkType.ApValleyFreeSymbolic:
        RunProfiler(Vf.AllPairsValleyFreeReachableSymbolicTimes(N));
        break;
      case BenchmarkType.FatTreeHijack:
        RunProfiler(Hijack.HijackFiltered(N, Destination, InferTimes));
        break;
      case BenchmarkType.FatTreeHijackSymbolic:
        RunProfiler(Hijack.HijackFilteredSymbolicTimes(N, Destination));
        break;
      case BenchmarkType.ApFatTreeHijack:
        RunProfiler(Hijack.AllPairsHijackFiltered(N));
        break;
      case BenchmarkType.ApFatTreeHijackSymbolic:
        RunProfiler(Hijack.AllPairsHijackFilteredSymbolicTimes(N));
        break;
      default:
        throw new ArgumentOutOfRangeException(null, Bench, "Invalid argument is not a benchmark type");
    }
  }

  private void RunProfiler<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net) where NodeType : notnull
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
  SpPathLengthSymbolic,
  ApReachable,
  ApReachableSymbolic,
  ApPathLength,
  ApPathLengthSymbolic,
  ValleyFree,
  ValleyFreeSymbolic,
  ValleyFreeLength,
  ApValleyFree,
  ApValleyFreeSymbolic,
  FatTreeHijack,
  FatTreeHijackSymbolic,
  ApFatTreeHijack,
  ApFatTreeHijackSymbolic
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
      "ls" or "lengthSymbolic" or "SpPathLengthSymbolic" => BenchmarkType.SpPathLengthSymbolic,
      "ar" or "allReach" or "ApReachable" => BenchmarkType.ApReachable,
      "ars" or "allReachSymbolic" or "ApReachableSymbolic" => BenchmarkType.ApReachableSymbolic,
      "al" or "allLength" or "ApPathLength" => BenchmarkType.ApPathLength,
      "als" or "allLengthSymbolic" or "ApPathLengthSymbolic" => BenchmarkType.ApPathLengthSymbolic,
      "v" or "valley" or "ValleyFree" => BenchmarkType.ValleyFree,
      "vs" or "valleySymbolic" or "ValleyFreeSymbolic" => BenchmarkType.ValleyFreeSymbolic,
      "vl" or "valleyLength" or "ValleyFreeLength" => BenchmarkType.ValleyFreeLength,
      "av" or "allValley" or "ApValleyFree" => BenchmarkType.ApValleyFree,
      "avs" or "allValleySymbolic" or "ApValleyFreeSymbolic" => BenchmarkType.ApValleyFreeSymbolic,
      "h" or "hijack" or "FatTreeHijack" => BenchmarkType.FatTreeHijack,
      "hs" or "hijackSymbolic" or "FatTreeHijackSymbolic" => BenchmarkType.FatTreeHijackSymbolic,
      "ah" or "allHijack" or "ApFatTreeHijack" => BenchmarkType.ApFatTreeHijack,
      "ahs" or "allHijackSymbolic" or "ApFatTreeHijackSymbolic" => BenchmarkType.ApFatTreeHijackSymbolic,
      _ => throw new ArgumentException($"{s} does not correspond to a valid BenchmarkType. Acceptable values:\n" +
                                       "- 'r'/'reach'/'SpReachable' for SpReachable\n" +
                                       "- 'rs'/'reachSymbolic'/'SpReachableSymbolic' for SpReachableSymbolic\n" +
                                       "- 'l'/'length'/'SpPathLength' for SpPathLength\n" +
                                       "- 'lws'/'lengthSymbolic'/'SpPathLengthSymbolic' for SpPathLengthSymbolic\n" +
                                       "- 'ar'/'allReach'/'ApReachable' for ApReachable\n" +
                                       "- 'ars'/'allReachSymbolic'/'ApReachableSymbolic' for ApReachableSymbolic\n" +
                                       "- 'al'/'allLength'/'ApPathLength' for ApPathLength\n" +
                                       "- 'als'/'allLengthSymbolic'/'ApPathLengthSymbolic' for ApPathLengthSymbolic\n" +
                                       "- 'v'/'valley'/'ValleyFree' for ValleyFree\n" +
                                       "- 'vs'/'valleySymbolic'/'ValleyFreeSymbolic' for ValleyFreeSymbolic\n" +
                                       "- 'vl'/'valleyLength'/'ValleyFreeLength' for ValleyFreeLength\n" +
                                       "- 'av'/'allValley'/'ApValleyFree' for ApValleyFree\n" +
                                       "- 'avs'/'allValleySymbolic'/'ApValleyFreeSymbolic' for ApValleyFreeSymbolic\n" +
                                       "- 'h'/'hijack'/'FatTreeHijack' for FatTreeHijack" +
                                       "- 'hs'/'hijackSymbolic'/'FatTreeHijackSymbolic' for FatTreeHijackSymbolic" +
                                       "- 'ah'/'allHijack'/'ApFatTreeHijack' for ApFatTreeHijack\n" +
                                       "- 'ahs'/'allHijackSymbolic'/'ApFatTreeHijackSymbolic' for ApFatTreeHijackSymbolic\n")
    };
  }

  public static bool HasSymbolicDestination(this BenchmarkType t)
  {
    return t switch
    {
      BenchmarkType.SpReachable => false,
      BenchmarkType.SpReachableSymbolic => false,
      BenchmarkType.SpPathLength => false,
      BenchmarkType.SpPathLengthSymbolic => false,
      BenchmarkType.ValleyFree => false,
      BenchmarkType.ValleyFreeLength => false,
      BenchmarkType.ValleyFreeSymbolic => false,
      BenchmarkType.FatTreeHijack => false,
      BenchmarkType.FatTreeHijackSymbolic => false,
      BenchmarkType.ApReachable => true,
      BenchmarkType.ApReachableSymbolic => true,
      BenchmarkType.ApPathLength => true,
      BenchmarkType.ApPathLengthSymbolic => true,
      BenchmarkType.ApValleyFree => true,
      BenchmarkType.ApValleyFreeSymbolic => true,
      BenchmarkType.ApFatTreeHijack => true,
      BenchmarkType.ApFatTreeHijackSymbolic => true,
      _ => throw new ArgumentOutOfRangeException(nameof(t), t,
        "Unable to determine if BenchmarkType is for a symbolic destination.")
    };
  }
}

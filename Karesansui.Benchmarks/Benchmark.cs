namespace Karesansui.Benchmarks;

public class Benchmark
{
  public Benchmark(uint n, string destination, BenchmarkType type, int timeout)
  {
    N = n;
    Destination = destination;
    Bench = type;
    Timeout = timeout;
  }

  public void Run()
  {
    switch (Bench)
    {
      case BenchmarkType.SpReachable:
        Profile.RunCmpPerNode(Sp.Reachability(Topologies.FatTree(N), Destination));
        break;
      case BenchmarkType.SpPathLength:
        Profile.RunCmpPerNode(Sp.PathLength(Topologies.FatTree(N), Destination));
        break;
      case BenchmarkType.ValleyFree:
        Profile.RunCmpPerNode(Vf.ValleyFreeReachable(Topologies.LabelledFatTree(N), Destination));
        break;
      case BenchmarkType.FatTreeHijack:
        Profile.RunCmpPerNode(Hijack.HijackFiltered(Topologies.FatTree(N), Destination));
        break;
      default:
        throw new ArgumentOutOfRangeException(null, Bench, "Invalid argument is not a benchmark type");
    }
  }

  public BenchmarkType Bench { get; set; }

  public string Destination { get; set; }

  public uint N { get; set; }

  public int Timeout { get; set; }
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

using Karesansui.Networks;
using ZenLib;

namespace Karesansui.Benchmarks;

public class Benchmark
{
  public Benchmark(uint n, string destination, BenchmarkType type)
  {
    N = n;
    Destination = destination;
    Bench = type;
  }

  public void Run()
  {
    Network<Option<BatfishBgpRoute>, Unit> net = Bench switch
    {
      BenchmarkType.SpReachable => Sp.Reachability(Topologies.FatTree(N), Destination),
      BenchmarkType.SpPathLength => Sp.PathLength(Topologies.FatTree(N), Destination),
      BenchmarkType.ValleyFree => Vf.ValleyFreeReachable(Topologies.LabelledFatTree(N), Destination),
      _ => throw new NotImplementedException()
    };
    Profile.RunCmpPerNode(net);
  }

  public BenchmarkType Bench { get; set; }

  public string Destination { get; set; }

  public uint N { get; set; }
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

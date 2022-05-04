using System.Numerics;
using System.Text.Json.Serialization;
using Timekeeper.Datatypes;
using ZenLib;

namespace Timekeeper.Json.TypedAst;

[ZenObject]
public class BatfishBgpRoute
{
  public BatfishBgpRoute()
  {
    DestinationPrefix = new Ipv4Prefix();
    AdminDist = 0;
    Lp = 0;
    AsPathLength = 0;
    Med = 0;
    OriginType = new Int2(0);
    Communities = new Set<string>();
  }

  [JsonConstructor]
  public BatfishBgpRoute(Ipv4Prefix destinationPrefix, uint adminDist, uint lp, BigInteger asPathLength, uint med, Int2 originType, Set<string> communities)
  {
    DestinationPrefix = destinationPrefix;
    AdminDist = adminDist;
    Lp = lp;
    AsPathLength = asPathLength;
    Med = med;
    OriginType = originType;
    Communities = communities;
  }

  /// <summary>
  /// IP prefix representing the routing destination.
  /// </summary>
  public Ipv4Prefix DestinationPrefix { get; set; }

  /// <summary>
  /// 32-bit integer representation of administrative distance.
  /// </summary>
  public uint AdminDist { get; set; }

  /// <summary>
  /// 32-bit integer representation of local preference.
  /// </summary>
  public uint Lp { get; set; }

  /// <summary>
  /// Integer representation of AS path length.
  /// </summary>
  public BigInteger AsPathLength { get; set; }

  /// <summary>
  /// 32-bit integer representation of the Multi-Exit Discriminator.
  /// </summary>
  public uint Med { get; set; }

  /// <summary>
  /// 2-bit integer representation of origin type.
  /// 0 or 1 = incomplete
  /// 2 = external
  /// 3 = internal
  /// </summary>
  public Int2 OriginType { get; set; }

  /// <summary>
  /// Representation of community tags as strings.
  /// </summary>
  public Set<string> Communities { get; set; }
}

public static class BatfishBgpRouteExtensions
{
  public static Zen<BatfishBgpRoute> Min(this Zen<BatfishBgpRoute> b1, Zen<BatfishBgpRoute> b2)
  {
    return Lang.CompareBy(b => b.GetLp(), Zen.Gt,
      Lang.CompareBy(b => b.GetAsPathLength(), Zen.Lt,
        Lang.CompareBy(b => b.GetOriginType(), Zen.Gt,
          Lang.CompareBy<BatfishBgpRoute, uint>(b => b.GetMed(), Zen.Lt))))(b1, b2);
  }

  public static Zen<Pair<bool, BatfishBgpRoute>> MinPair(this Zen<Pair<bool, BatfishBgpRoute>> b1,
    Zen<Pair<bool, BatfishBgpRoute>> b2)
  {
    return Zen.If(Zen.Not(b1.Item1()), b2,
      Zen.If(Zen.Not(b2.Item1()), b1, Pair.Create(Zen.True(), Min(b1.Item2(), b2.Item2()))));
  }
}

using System.Numerics;
using System.Text.Json.Serialization;
using Timepiece.Datatypes;
using ZenLib;

namespace Timepiece.Angler;

[ZenObject]
public class BatfishBgpRoute
{
  public BatfishBgpRoute()
  {
    Prefix = new Ipv4Prefix();
    AdminDist = 0;
    Lp = 0;
    AsPathLength = 0;
    Med = 0;
    OriginType = new Int<_2>(0);
    Communities = new CSet<string>();
  }

  [JsonConstructor]
  public BatfishBgpRoute(Ipv4Prefix prefix, uint adminDist, uint lp, BigInteger asPathLength, uint med,
    Int<_2> originType, CSet<string> communities)
  {
    Prefix = prefix;
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
  public Ipv4Prefix Prefix { get; set; }

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
  public Int<_2> OriginType { get; set; }

  /// <summary>
  /// Representation of community tags as strings.
  /// </summary>
  public CSet<string> Communities { get; set; }
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

  public static Zen<BatfishBgpRoute> IncrementAsPathLength(this Zen<BatfishBgpRoute> b, Zen<BigInteger> x) =>
    b.WithAsPathLength(b.GetAsPathLength() + x);
}
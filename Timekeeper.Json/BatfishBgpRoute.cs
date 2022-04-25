using System.Numerics;
using System.Text.Json.Serialization;
using ZenLib;

namespace Gardener;

public record struct BatfishBgpRoute
{
  public BatfishBgpRoute()
  {
    AdminDist = 0;
    Lp = 0;
    AsPathLength = 0;
    Med = 0;
    OriginType = new Int2(0);
    // TODO: how to set the maximum depth?
    Communities = new Set<string>();
  }

  [JsonConstructor]
  public BatfishBgpRoute(uint adminDist, uint lp, uint asPathLength, uint med, Int2 originType, Set<string> communities)
  {
    AdminDist = adminDist;
    Lp = lp;
    AsPathLength = asPathLength;
    Med = med;
    OriginType = originType;
    Communities = communities;
  }

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
  public static Zen<uint> GetLp(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, uint>("Lp");
  }

  public static Zen<BatfishBgpRoute> WithLp(this Zen<BatfishBgpRoute> b, Zen<uint> lp)
  {
    return b.WithField("Lp", lp);
  }

  public static Zen<BigInteger> GetAsPathLength(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, BigInteger>("AsPathLength");
  }

  public static Zen<BatfishBgpRoute> WithAsPathLength(this Zen<BatfishBgpRoute> b, Zen<BigInteger> asPathLength)
  {
    return b.WithField("AsPathLength", asPathLength);
  }

  public static Zen<uint> GetMed(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, uint>("Med");
  }

  public static Zen<BatfishBgpRoute> WithMed(this Zen<BatfishBgpRoute> b, Zen<uint> med)
  {
    return b.WithField("Med", med);
  }

  public static Zen<Int2> GetOriginType(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, Int2>("OriginType");
  }

  public static Zen<BatfishBgpRoute> WithOriginType(this Zen<BatfishBgpRoute> b, Zen<Int2> originType)
  {
    return b.WithField("OriginType", originType);
  }

  public static Zen<Set<string>> GetCommunities(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, Set<string>>("Communities");
  }

  public static Zen<BatfishBgpRoute> WithCommunities(this Zen<BatfishBgpRoute> b, Zen<Set<string>> communities)
  {
    return b.WithField("Communities", communities);
  }

  private static Func<Zen<T>, Zen<T>, Zen<T>> MinBy<T, TKey>(Func<Zen<T>, Zen<TKey>> keyAccessor,
    Func<Zen<TKey>, Zen<TKey>, Zen<bool>> keyComparator)
  {
    return (t1, t2) => Zen.If(keyComparator(keyAccessor(t1), keyAccessor(t2)), t1, t2);
  }

  public static Zen<BatfishBgpRoute> Min(this Zen<BatfishBgpRoute> b1, Zen<BatfishBgpRoute> b2)
  {
    var largerLp = MinBy<BatfishBgpRoute, uint>(GetLp, Zen.Gt);
    var smallerLength = MinBy<BatfishBgpRoute, BigInteger>(GetAsPathLength, Zen.Lt);
    var betterOrigin = MinBy<BatfishBgpRoute, Int2>(GetOriginType, Zen.Gt);
    var lowerMed = MinBy<BatfishBgpRoute, uint>(GetMed, Zen.Lt);
    return largerLp(b1, smallerLength(b1, betterOrigin(b1, lowerMed(b1, b2))));
    // return Zen.If(Zen.Not(b1.IsValid()), b2,
    // Zen.If(Zen.Not(b2.IsValid()), b1,
    // largerLp(b1, smallerLength(b1, betterOrigin(b1, lowerMed(b1, b2))))));
  }

  public static Zen<Pair<bool, BatfishBgpRoute>> MinPair(this Zen<Pair<bool, BatfishBgpRoute>> b1,
    Zen<Pair<bool, BatfishBgpRoute>> b2)
  {
    return Zen.If(Zen.Not(b1.Item1()), b2,
      Zen.If(Zen.Not(b2.Item1()), b1, Pair.Create(Zen.True(), Min(b1.Item2(), b2.Item2()))));
  }
}

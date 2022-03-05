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
  }

  public BatfishBgpRoute(int adminDist, int lp, int asPathLength, int med, Int2 originType)
  {
    AdminDist = adminDist;
    Lp = lp;
    AsPathLength = asPathLength;
    Med = med;
    OriginType = originType;
  }

  /// <summary>
  /// 32-bit integer representation of administrative distance.
  /// </summary>
  public int AdminDist { get; set; }

  /// <summary>
  /// 32-bit integer representation of local preference.
  /// </summary>
  public int Lp { get; set; }

  /// <summary>
  /// 32-bit integer representation of AS path length.
  /// </summary>
  public int AsPathLength { get; set; }

  /// <summary>
  /// 32-bit integer representation of the Multi-Exit Discriminator.
  /// </summary>
  public int Med { get; set; }

  /// <summary>
  /// 2-bit integer representation of origin type.
  /// 0 or 1 = incomplete
  /// 2 = external
  /// 3 = internal
  /// </summary>
  public Int2 OriginType { get; set; }
}

public static class BatfishBgpRouteExtensions
{
  public static Zen<int> GetLp(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, int>("Lp");
  }

  public static Zen<int> GetAsPathLength(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, int>("AsPathLength");
  }

  public static Zen<int> GetMed(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, int>("Med");
  }

  public static Zen<Int2> GetOriginType(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, Int2>("OriginType");
  }

  private static Func<Zen<T>, Zen<T>, Zen<T>> MinBy<T, TKey>(Func<Zen<T>, Zen<TKey>> keyAccessor,
    Func<Zen<TKey>, Zen<TKey>, Zen<bool>> keyComparator)
  {
    return (t1, t2) => Zen.If(keyComparator(keyAccessor(t1), keyAccessor(t2)), t1, t2);
  }

  public static Zen<BatfishBgpRoute> Min(this Zen<BatfishBgpRoute> b1, Zen<BatfishBgpRoute> b2)
  {
    var largerLp = MinBy<BatfishBgpRoute, int>(GetLp, Zen.Gt);
    var smallerLength = MinBy<BatfishBgpRoute, int>(GetAsPathLength, Zen.Lt);
    var betterOrigin = MinBy<BatfishBgpRoute, Int2>(GetOriginType, Zen.Gt);
    var lowerMed = MinBy<BatfishBgpRoute, int>(GetMed, Zen.Lt);
    return largerLp(b1, smallerLength(b1, betterOrigin(b1, lowerMed(b1, b2))));
  }
}

using System.Numerics;
using System.Text;
using ZenLib;

namespace Timekeeper.Benchmarks;

public record struct SimpleBgpRoute
{
  public SimpleBgpRoute()
  {
    AdminDist = 0;
    Lp = 100;
    AsPathLength = BigInteger.Zero;
    Med = 0;
    OriginType = new UInt2(0);
  }

  public uint AdminDist { get; set; }

  public uint Lp { get; set; }

  public BigInteger AsPathLength { get; set; }

  public uint Med { get; set; }

  public UInt2 OriginType { get; set; }

  public override string ToString()
  {
    var sb = new StringBuilder();
    sb.Append(
      $"SimpleBgpRoute {{ AdminDist = {AdminDist}, Lp = {Lp}, AsPathLength = {AsPathLength}, Med = {Med}, OriginType = {OriginType.ToLong()} }}");
    return sb.ToString();
  }
}

public static class SimpleBgpRouteExtensions
{
  public static Zen<uint> GetLp(this Zen<SimpleBgpRoute> b)
  {
    return b.GetField<SimpleBgpRoute, uint>("Lp");
  }

  public static Zen<SimpleBgpRoute> WithLp(this Zen<SimpleBgpRoute> b, Zen<uint> lp)
  {
    return b.WithField("Lp", lp);
  }

  public static Zen<BigInteger> GetAsPathLength(this Zen<SimpleBgpRoute> b)
  {
    return b.GetField<SimpleBgpRoute, BigInteger>("AsPathLength");
  }

  public static Zen<SimpleBgpRoute> WithAsPathLength(this Zen<SimpleBgpRoute> b, Zen<BigInteger> asPathLength)
  {
    return b.WithField("AsPathLength", asPathLength);
  }

  public static Zen<uint> GetMed(this Zen<SimpleBgpRoute> b)
  {
    return b.GetField<SimpleBgpRoute, uint>("Med");
  }

  public static Zen<SimpleBgpRoute> WithMed(this Zen<SimpleBgpRoute> b, Zen<uint> med)
  {
    return b.WithField("Med", med);
  }

  public static Zen<UInt2> GetOriginType(this Zen<SimpleBgpRoute> b)
  {
    return b.GetField<SimpleBgpRoute, UInt2>("OriginType");
  }

  public static Zen<SimpleBgpRoute> WithOriginType(this Zen<SimpleBgpRoute> b, Zen<UInt2> originType)
  {
    return b.WithField("OriginType", originType);
  }

  /// <summary>
  /// Compare two SimpleBgpRoutes and return the minimum.
  /// Ranking is done in the following order:
  /// 1. Greatest local preference.
  /// 2. Smallest AS path length.
  /// 3. Best (greatest) origin type.
  /// 4. Smallest MED.
  /// </summary>
  /// <param name="b1">The first route.</param>
  /// <param name="b2">The second route.</param>
  /// <returns>The minimum route by the ranking.</returns>
  public static Zen<SimpleBgpRoute> Min(this Zen<SimpleBgpRoute> b1, Zen<SimpleBgpRoute> b2)
  {
    return Lang.CompareBy(GetLp, Zen.Gt,
      Lang.CompareBy(GetAsPathLength, Zen.Lt,
        Lang.CompareBy(GetOriginType, Zen.Gt,
          Lang.CompareBy<SimpleBgpRoute, uint>(GetMed, Zen.Lt))))(b1, b2);
  }

  public static Zen<SimpleBgpRoute> IncrementAsPath(this Zen<SimpleBgpRoute> b)
  {
    return b.WithAsPathLength(b.GetAsPathLength() + BigInteger.One);
  }

  /// <summary>
  /// Return true if the AS path length is a non-negative number at most x, and false otherwise.
  /// </summary>
  /// <param name="b">The route.</param>
  /// <param name="x">The maximum length.</param>
  /// <returns>True if the AS path length of b is at most x, false otherwise.</returns>
  public static Zen<bool> LengthAtMost(this Zen<SimpleBgpRoute> b, Zen<BigInteger> x) =>
    Zen.And(b.GetAsPathLength() <= x, b.GetAsPathLength() >= BigInteger.Zero);

  /// <summary>
  /// Return true if the LP equals lp, and false otherwise.
  /// </summary>
  /// <param name="b"></param>
  /// <param name="lp"></param>
  /// <returns></returns>
  public static Zen<bool> LpEquals(this Zen<SimpleBgpRoute> b, Zen<uint> lp) => b.GetLp() == lp;

  public static Func<Zen<SimpleBgpRoute>, Zen<bool>> MaxLengthDefaultLp(BigInteger x) =>
    b => Zen.And(b.LengthAtMost(x), b.LpEquals(100));

  public static Func<Zen<SimpleBgpRoute>, Zen<bool>> EqLengthDefaultLp(BigInteger x) =>
    b => Zen.And(b.GetAsPathLength() == x, b.LpEquals(100));
}

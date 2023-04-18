using System.Numerics;
using System.Text;
using ZenLib;

namespace Timepiece.Benchmarks;

/// <summary>
///   A simpler model of a BGP route.
///   Excludes prefixes and community tags.
/// </summary>
[ZenObject]
public class SimpleBgpRoute
{
  public SimpleBgpRoute()
  {
    AdminDist = 0;
    Lp = 100;
    AsPathLength = BigInteger.Zero;
    Med = 0;
    OriginType = new UInt<_2>(0);
  }

  public uint AdminDist { get; set; }

  public uint Lp { get; set; }

  public BigInteger AsPathLength { get; set; }

  public uint Med { get; set; }

  public UInt<_2> OriginType { get; set; }

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
  /// <summary>
  ///   Compare two SimpleBgpRoutes and return the minimum.
  ///   Ranking is done in the following order:
  ///   1. Greatest local preference.
  ///   2. Smallest AS path length.
  ///   3. Best (greatest) origin type.
  ///   4. Smallest MED.
  /// </summary>
  /// <param name="b1">The first route.</param>
  /// <param name="b2">The second route.</param>
  /// <returns>The minimum route by the ranking.</returns>
  public static Zen<SimpleBgpRoute> Min(this Zen<SimpleBgpRoute> b1, Zen<SimpleBgpRoute> b2)
  {
    return Lang.CompareBy(b => b.GetLp(), Zen.Gt,
      Lang.CompareBy(b => b.GetAsPathLength(), Zen.Lt,
        Lang.CompareBy(b => b.GetOriginType(), Zen.Gt,
          Lang.CompareBy<SimpleBgpRoute, uint>(b => b.GetMed(), Zen.Lt))))(b1, b2);
  }

  public static Zen<SimpleBgpRoute> IncrementAsPath(this Zen<SimpleBgpRoute> b)
  {
    return b.WithAsPathLength(b.GetAsPathLength() + BigInteger.One);
  }

  /// <summary>
  ///   Return true if the AS path length is a non-negative number at most x, and false otherwise.
  /// </summary>
  /// <param name="b">The route.</param>
  /// <param name="x">The maximum length.</param>
  /// <returns>True if the AS path length of b is at most x, false otherwise.</returns>
  public static Zen<bool> LengthAtMost(this Zen<SimpleBgpRoute> b, Zen<BigInteger> x)
  {
    return Zen.And(b.GetAsPathLength() <= x, b.GetAsPathLength() >= BigInteger.Zero);
  }

  /// <summary>
  ///   Return true if the LP equals lp, and false otherwise.
  /// </summary>
  /// <param name="b"></param>
  /// <param name="lp"></param>
  /// <returns></returns>
  public static Zen<bool> LpEquals(this Zen<SimpleBgpRoute> b, Zen<uint> lp)
  {
    return b.GetLp() == lp;
  }

  public static Func<Zen<SimpleBgpRoute>, Zen<bool>> MaxLengthDefaultLp(BigInteger x)
  {
    return b => Zen.And(b.LengthAtMost(x), b.LpEquals(100));
  }

  public static Func<Zen<SimpleBgpRoute>, Zen<bool>> EqLengthDefaultLp(BigInteger x)
  {
    return b => Zen.And(b.GetAsPathLength() == x, b.LpEquals(100));
  }
}

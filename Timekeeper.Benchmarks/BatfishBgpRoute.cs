using System.Numerics;
using System.Text;
using ZenLib;

namespace Karesansui.Benchmarks;

public record struct BatfishBgpRoute
{
  public BatfishBgpRoute()
  {
    Destination = 0;
    AdminDist = 0;
    Lp = 100;
    AsPathLength = 0;
    Med = 0;
    OriginType = new UInt2(0);
    Communities = new Set<string>();
  }

  public BatfishBgpRoute(uint adminDist, uint lp, uint asPathLength, uint med, UInt2 originType,
    Set<string> communities, uint destination)
  {
    AdminDist = adminDist;
    Lp = lp;
    AsPathLength = asPathLength;
    Med = med;
    OriginType = originType;
    Communities = communities;
    Destination = destination;
  }

  /// <summary>
  /// Integer representation of destination IPv4 address.
  /// </summary>
  public uint Destination { get; set; }

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
  public UInt2 OriginType { get; set; }

  /// <summary>
  /// Representation of community tags as strings.
  /// </summary>
  public Set<string> Communities { get; set; }

  public override string ToString()
  {
    var sb = new StringBuilder();
    sb.Append(
      $"BatfishBgpRoute {{ Destination = {Destination}, AdminDist = {AdminDist}, Lp = {Lp}, AsPathLength = {AsPathLength}, Med = {Med}, OriginType = {OriginType.ToLong()}, Communities = {Communities} }}");
    return sb.ToString();
  }
}

public static class BatfishBgpRouteExtensions
{
  public static Zen<BatfishBgpRoute> ToDestination(Zen<uint> destination)
  {
    Zen<BatfishBgpRoute> b = new BatfishBgpRoute();
    return b.WithDestination(destination);
  }

  public static Zen<uint> GetDestination(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, uint>("Destination");
  }

  public static Zen<BatfishBgpRoute> WithDestination(this Zen<BatfishBgpRoute> b, Zen<uint> destination)
  {
    return b.WithField("Destination", destination);
  }

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

  public static Zen<UInt2> GetOriginType(this Zen<BatfishBgpRoute> b)
  {
    return b.GetField<BatfishBgpRoute, UInt2>("OriginType");
  }

  public static Zen<BatfishBgpRoute> WithOriginType(this Zen<BatfishBgpRoute> b, Zen<UInt2> originType)
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

  /// <summary>
  /// Compare two BatfishBgpRoutes and return the minimum.
  /// Ranking is done in the following order:
  /// 1. Greatest local preference.
  /// 2. Smallest AS path length.
  /// 3. Best (greatest) origin type.
  /// 4. Smallest MED.
  /// </summary>
  /// <param name="b1">The first route.</param>
  /// <param name="b2">The second route.</param>
  /// <returns>The minimum route by the ranking.</returns>
  public static Zen<BatfishBgpRoute> Min(this Zen<BatfishBgpRoute> b1, Zen<BatfishBgpRoute> b2)
  {
    return Lang.CompareBy(GetLp, Zen.Gt,
      Lang.CompareBy(GetAsPathLength, Zen.Lt,
        Lang.CompareBy(GetOriginType, Zen.Gt,
          Lang.CompareBy<BatfishBgpRoute, uint>(GetMed, Zen.Lt))))(b1, b2);
  }

  /// <summary>
  /// Pick the minimum route, restricting to routes to a specific destination.
  /// Routes to other destinations are ignored (choice is unspecified).
  /// </summary>
  /// <param name="b1"></param>
  /// <param name="b2"></param>
  /// <param name="destination">The destination both routes should be routing towards.</param>
  /// <returns></returns>
  public static Zen<BatfishBgpRoute> MinPrefix(this Zen<BatfishBgpRoute> b1, Zen<BatfishBgpRoute> b2,
    Zen<uint> destination)
  {
    var d1 = b1.GetDestination();
    var d2 = b2.GetDestination();
    return Zen.If(Zen.And(d1 == destination, d2 == destination), Min(b1, b2),
      Zen.If(d1 == destination, b1, b2));
  }

  public static Zen<BatfishBgpRoute> IncrementAsPath(this Zen<BatfishBgpRoute> b)
  {
    return b.WithAsPathLength(b.GetAsPathLength() + BigInteger.One);
  }

  public static Zen<bool> HasCommunity(this Zen<BatfishBgpRoute> b, Zen<string> comm)
  {
    return b.GetCommunities().Contains(comm);
  }

  public static Zen<BatfishBgpRoute> AddCommunity(this Zen<BatfishBgpRoute> b, Zen<string> comm)
  {
    return b.WithCommunities(b.GetCommunities().Add(comm));
  }

  public static Zen<Option<BatfishBgpRoute>> FilterCommunity(this Zen<BatfishBgpRoute> b, Zen<string> comm)
  {
    return Zen.If(b.HasCommunity(comm), Option.None<BatfishBgpRoute>(), Option.Create(b));
  }

  /// <summary>
  /// Return true if the AS path length is a non-negative number at most x, and false otherwise.
  /// </summary>
  /// <param name="b">The route.</param>
  /// <param name="x">The maximum length.</param>
  /// <returns>True if the AS path length of b is at most x, false otherwise.</returns>
  public static Zen<bool> LengthAtMost(this Zen<BatfishBgpRoute> b, Zen<BigInteger> x) =>
    Zen.And(b.GetAsPathLength() <= x, b.GetAsPathLength() >= BigInteger.Zero);

  /// <summary>
  /// Return true if the LP equals lp, and false otherwise.
  /// </summary>
  /// <param name="b"></param>
  /// <param name="lp"></param>
  /// <returns></returns>
  public static Zen<bool> LpEquals(this Zen<BatfishBgpRoute> b, Zen<uint> lp) => b.GetLp() == lp;

  /// <summary>
  /// Return true if the destination is as given.
  /// </summary>
  /// <param name="b"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  public static Zen<bool> DestinationIs(this Zen<BatfishBgpRoute> b, Zen<uint> destination) =>
    b.GetDestination() == destination;

  public static Func<Zen<BatfishBgpRoute>, Zen<bool>> MaxLengthDefaultLp(BigInteger x) =>
    b => Zen.And(b.LengthAtMost(x), b.LpEquals(100));

  public static Func<Zen<BatfishBgpRoute>, Zen<bool>> EqLengthDefaultLp(BigInteger x) =>
    b => Zen.And(b.GetAsPathLength() == x, b.LpEquals(100));
}

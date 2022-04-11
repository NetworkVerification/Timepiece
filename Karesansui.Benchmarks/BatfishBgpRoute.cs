using System.Numerics;
using System.Text;
using Microsoft.Z3;
using ZenLib;

namespace Karesansui.Benchmarks;

public record struct BatfishBgpRoute
{
  public BatfishBgpRoute()
  {
    AdminDist = 0;
    Lp = 0;
    AsPathLength = 0;
    Med = 0;
    OriginType = new UInt2(0);
    // TODO: how to set the maximum depth?
    Communities = new Set<string>();
  }

  public BatfishBgpRoute(uint adminDist, uint lp, uint asPathLength, uint med, UInt2 originType,
    Set<string> communities)
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
  public UInt2 OriginType { get; set; }

  /// <summary>
  /// Representation of community tags as strings.
  /// </summary>
  public Set<string> Communities { get; set; }

  public override string ToString()
  {
    var sb = new StringBuilder();
    sb.Append(
      $"BatfishBgpRoute {{ AdminDist = {AdminDist}, Lp = {Lp}, AsPathLength = {AsPathLength}, Med = {Med}, OriginType = {OriginType.ToLong()}, Communities = {Communities} }}");
    return sb.ToString();
  }
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
  /// Return a function comparing two objects of type T using the specified key accessor and the specified key comparator.
  /// If the comparator returns true given object1 and object2, object1 is returned.
  /// If the comparator returns false given object1 and object2, it tests object2 and object1 (in reverse order).
  /// If the second comparison returns true, object2 is returned.
  /// If the second comparison returns false, the fallthrough is executed on the two objects.
  /// (A natural fallthrough in this case would be to return the second object, thereby replicating an If.
  /// This method's benefit comes from using allowing us to chain comparisons in sequence.)
  /// </summary>
  /// <param name="keyAccessor">The function used to access the objects' keys.</param>
  /// <param name="keyComparator">The function used to compare the keys.</param>
  /// <param name="fallThrough">The function to call if the keyComparator returns false.</param>
  /// <typeparam name="T">The type of objects.</typeparam>
  /// <typeparam name="TKey">The type of keys.</typeparam>
  /// <returns>
  /// A function returning the first object if the comparator evaluates to true, and otherwise calling the fallthrough.
  /// </returns>
  private static Func<Zen<T>, Zen<T>, Zen<T>> CompareBy<T, TKey>(
    Func<Zen<T>, Zen<TKey>> keyAccessor,
    Func<Zen<TKey>, Zen<TKey>, Zen<bool>> keyComparator,
    Func<Zen<T>, Zen<T>, Zen<T>> fallThrough)
  {
    return (t1, t2) => Zen.If(keyComparator(keyAccessor(t1), keyAccessor(t2)), t1,
      Zen.If(keyComparator(keyAccessor(t2), keyAccessor(t1)), t2, fallThrough(t1, t2)));
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
    // var returnSecond = new Func<Zen<BatfishBgpRoute>, Zen<BatfishBgpRoute>, Zen<BatfishBgpRoute>>((_, t2) => t2);
    // return CompareBy(GetLp, Zen.Gt,
    // CompareBy(GetAsPathLength, Zen.Lt, CompareBy(GetOriginType, Zen.Gt, CompareBy(GetMed, Zen.Lt, returnSecond))))(b1,
    // b2);
    return Zen.If(b1.GetLp() > b2.GetLp(), b1,
      Zen.If(b2.GetLp() > b1.GetLp(), b2,
        Zen.If(b1.GetAsPathLength() < b2.GetAsPathLength(), b1,
          Zen.If(b2.GetAsPathLength() < b1.GetAsPathLength(), b2,
            Zen.If(b1.GetOriginType() > b2.GetOriginType(), b1,
              Zen.If(b2.GetOriginType() > b1.GetOriginType(), b2,
                Zen.If(b1.GetMed() < b2.GetMed(), b1, b2)))))));
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

  public static Func<Zen<BatfishBgpRoute>, Zen<bool>> MaxLengthZeroLp(BigInteger x) =>
    b => Zen.And(b.LengthAtMost(x), b.LpEquals(0));
}

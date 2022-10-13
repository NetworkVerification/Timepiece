using System.Numerics;
using System.Text.Json.Serialization;
using Timepiece.Datatypes;
using ZenLib;

namespace Timepiece.Angler;

[ZenObject]
public class RouteEnvironment
{
  public RouteEnvironment()
  {
    Returned = false;
    FallThrough = false;
    Exited = false;
    Value = false;
    Prefix = new Ipv4Prefix();
    Weight = 0;
    Lp = 0;
    AsPathLength = 0;
    Med = 0;
    OriginType = new Int<_2>(0);
    Communities = new Set<string>();
  }

  [JsonConstructor]
  public RouteEnvironment(Ipv4Prefix prefix, uint weight, uint lp, BigInteger asPathLength, uint med,
    Int<_2> originType, Set<string> communities, bool returned, bool fallThrough, bool exited, bool value)
  {
    Prefix = prefix;
    Weight = weight;
    Lp = lp;
    AsPathLength = asPathLength;
    Med = med;
    OriginType = originType;
    Communities = communities;
    Returned = returned;
    FallThrough = fallThrough;
    Exited = exited;
    Value = value;
  }

  public bool Value { get; set; }

  public bool Exited { get; set; }

  public bool FallThrough { get; set; }

  public bool Returned { get; set; }

  /// <summary>
  /// IP prefix representing the routing destination.
  /// </summary>
  public Ipv4Prefix Prefix { get; set; }

  /// <summary>
  /// 32-bit integer representation of administrative distance.
  /// </summary>
  public uint Weight { get; set; }

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
  public Set<string> Communities { get; set; }
}

public static class RouteEnvironmentExtensions
{
  public static Zen<RouteEnvironment> Min(this Zen<RouteEnvironment> b1, Zen<RouteEnvironment> b2)
  {
    return Lang.CompareBy(b => b.GetLp(), Zen.Gt,
      Lang.CompareBy(b => b.GetAsPathLength(), Zen.Lt,
        Lang.CompareBy(b => b.GetOriginType(), Zen.Gt,
          Lang.CompareBy<RouteEnvironment, uint>(b => b.GetMed(), Zen.Lt))))(b1, b2);
  }

  public static Zen<RouteEnvironment> MinOptional(this Zen<RouteEnvironment> b1, Zen<RouteEnvironment> b2)
  {
    return Zen.If(Zen.Not(b1.GetValue()), b2,
      Zen.If(Zen.Not(b2.GetValue()), b1, Min(b1, b2)));
  }

  public static Zen<RouteEnvironment> IncrementAsPathLength(this Zen<RouteEnvironment> b, Zen<BigInteger> x) =>
    b.WithAsPathLength(b.GetAsPathLength() + x);
}

using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Timepiece.Datatypes;
using ZenLib;

namespace Timepiece.Angler;

[ZenObject]
public class RouteEnvironment
{
  public RouteEnvironment()
  {
    Prefix = new Ipv4Prefix();
    Weight = 32768;
    Lp = 100;
    AsPathLength = 0;
    Metric = 0;
    Tag = 0;
    OriginType = new UInt<_2>(0);
    Communities = new CSet<string>();
    Result = new RouteResult();
    LocalDefaultAction = false;
  }


  [JsonConstructor]
  public RouteEnvironment(Ipv4Prefix prefix, uint weight, uint lp, BigInteger asPathLength, uint metric, uint tag,
    UInt<_2> originType, CSet<string> communities, RouteResult result,
    bool localDefaultAction)
  {
    Prefix = prefix;
    Weight = weight;
    Lp = lp;
    AsPathLength = asPathLength;
    Metric = metric;
    Tag = tag;
    OriginType = originType;
    Communities = communities;
    Result = result;
    LocalDefaultAction = localDefaultAction;
  }


  public RouteResult Result { get; set; }

  public bool LocalDefaultAction { get; set; }

  /// <summary>
  /// IP prefix representing the routing destination.
  /// </summary>
  public Ipv4Prefix Prefix { get; set; }

  /// <summary>
  /// 32-bit integer representation of administrative distance.
  /// Used by Cisco.
  /// https://www.networkers-online.com/blog/2012/05/bgp-weight/
  /// Defaults to 32768 (2^15).
  /// </summary>
  public uint Weight { get; set; }

  /// <summary>
  /// 32-bit integer representation of local preference.
  /// Defaults to 100.
  /// </summary>
  public uint Lp { get; set; }

  /// <summary>
  /// Integer representation of AS path length.
  /// </summary>
  public BigInteger AsPathLength { get; set; }

  /// <summary>
  /// 32-bit integer representation of the Multi-Exit Discriminator.
  /// </summary>
  public uint Metric { get; set; }

  /// <summary>
  /// 2-bit integer representation of origin type.
  /// 0 or 1 = incomplete
  /// 2 = external
  /// 3 = internal
  /// </summary>
  public UInt<_2> OriginType { get; set; }

  /// <summary>
  /// https://packetlife.net/blog/2009/jan/19/bgp-route-auto-tagging/
  /// </summary>
  public uint Tag { get; set; }

  /// <summary>
  /// Representation of community tags as strings.
  /// </summary>
  public CSet<string> Communities { get; set; }

  public override string ToString()
  {
    var properties = typeof(RouteEnvironment).GetProperties();
    var propertiesBuilder = new StringBuilder();
    // add each property to the builder
    foreach (var property in properties)
    {
      if (propertiesBuilder.Length > 0)
      {
        propertiesBuilder.Append(", ");
      }

      switch (property.PropertyType.Name)
      {
        case "CSet":
          var setString = string.Empty;
          foreach (var s in Communities.Map.Values.Keys)
          {
            if (string.IsNullOrEmpty(setString))
              setString += $"{s}";
            else
              setString += $", {s}";
          }

          propertiesBuilder.Append(setString);
          break;
        default:
          propertiesBuilder.Append($"{property.Name}={property.GetValue(this)}");
          break;
      }
    }

    return $"RouteEnvironment({propertiesBuilder})";
  }
}

public static class RouteEnvironmentExtensions
{
  /// <summary>
  /// Compare two routes by their fields in the following order:
  /// 1. Greatest weight (administrative distance).
  /// 2. Greatest LP (local preference).
  /// 3. Shortest AS path length.
  /// 4. Greatest origin type.
  /// 5. Lowest metric.
  /// </summary>
  /// <param name="b1"></param>
  /// <param name="b2"></param>
  /// <returns></returns>
  public static Zen<RouteEnvironment> Min(this Zen<RouteEnvironment> b1, Zen<RouteEnvironment> b2)
  {
    return Lang.CompareBy(b => b.GetWeight(), Zen.Gt,
      Lang.CompareBy(b => b.GetLp(), Zen.Gt,
        Lang.CompareBy(b => b.GetAsPathLength(), Zen.Lt,
          Lang.CompareBy(b => b.GetOriginType(), Zen.Gt,
            Lang.CompareBy<RouteEnvironment, uint>(b => b.GetMetric(), Zen.Lt)))))(b1, b2);
  }

  public static Zen<RouteEnvironment> MinOptional(this Zen<RouteEnvironment> b1, Zen<RouteEnvironment> b2)
  {
    return Zen.If(Zen.Not(b1.GetResult().GetValue()), b2,
      Zen.If(Zen.Not(b2.GetResult().GetValue()), b1, Min(b1, b2)));
  }

  public static Zen<RouteEnvironment> IncrementAsPathLength(this Zen<RouteEnvironment> b, Zen<BigInteger> x) =>
    b.WithAsPathLength(b.GetAsPathLength() + x);
}

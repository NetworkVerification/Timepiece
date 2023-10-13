using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler;

/// <summary>
/// A representation of a route and the current routing environment:
/// the results of executing the policy, and the current local default action.
/// </summary>
[ZenObject]
public class RouteEnvironment : DifferentiatedString<RouteEnvironment>
{
  public static readonly UInt<_2> IncompleteOrigin = new(0);
  public static readonly UInt<_2> ExternalOrigin = new(2);
  public static readonly UInt<_2> InternalOrigin = new(3);

  public const uint DefaultLp = 100;
  public const uint DefaultWeight = 32768;
  public const uint DefaultMetric = 0;

  public RouteEnvironment()
  {
    Prefix = new Ipv4Prefix();
    Weight = DefaultWeight;
    Lp = DefaultLp;
    AsPathLength = 0;
    Metric = DefaultMetric;
    Tag = 0;
    OriginType = IncompleteOrigin;
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
  ///   IP prefix representing the routing destination.
  /// </summary>
  public Ipv4Prefix Prefix { get; set; }

  /// <summary>
  ///   32-bit integer representation of administrative distance.
  ///   Used by Cisco.
  ///   https://www.networkers-online.com/blog/2012/05/bgp-weight/
  ///   Defaults to 32768 (2^15).
  /// </summary>
  public uint Weight { get; set; }

  /// <summary>
  ///   32-bit integer representation of local preference.
  ///   Defaults to 100.
  /// </summary>
  public uint Lp { get; set; }

  /// <summary>
  ///   Integer representation of AS path length.
  /// </summary>
  public BigInteger AsPathLength { get; set; }

  /// <summary>
  ///   32-bit integer representation of the Multi-Exit Discriminator.
  /// </summary>
  public uint Metric { get; set; }

  /// <summary>
  ///   2-bit integer representation of origin type.
  ///   0 or 1 = incomplete
  ///   2 = external
  ///   3 = internal
  /// </summary>
  public UInt<_2> OriginType { get; set; }

  /// <summary>
  ///   https://packetlife.net/blog/2009/jan/19/bgp-route-auto-tagging/
  /// </summary>
  public uint Tag { get; set; }

  /// <summary>
  ///   Representation of community tags as strings.
  /// </summary>
  public CSet<string> Communities { get; set; }

  public override string ToString()
  {
    var properties = typeof(RouteEnvironment).GetProperties();
    var propertiesBuilder = new StringBuilder();
    // add each property to the builder
    foreach (var property in properties)
    {
      if (propertiesBuilder.Length > 0) propertiesBuilder.Append(", ");

      switch (property.PropertyType.Name)
      {
        case "CSet":
          propertiesBuilder.AppendJoin(", ", Communities.Map.Values.Keys);
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
  ///   Compare two routes by their fields in the following order:
  ///   1. Greatest weight (administrative distance).
  ///   2. Greatest LP (local preference).
  ///   3. Shortest AS path length.
  ///   4. Greatest origin type.
  ///   5. Lowest metric.
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
    return Zen.If(Zen.Not(b1.GetResultValue()), b2,
      Zen.If(Zen.Not(b2.GetResultValue()), b1, Min(b1, b2)));
  }

  public static Zen<RouteEnvironment> IncrementAsPathLength(this Zen<RouteEnvironment> b, Zen<BigInteger> x)
  {
    return b.WithAsPathLength(b.GetAsPathLength() + x);
  }

  public static Zen<bool> HasCommunity(this Zen<RouteEnvironment> b, string community)
  {
    return b.GetCommunities().Contains(community);
  }

  public static Zen<bool> GetResultValue(this Zen<RouteEnvironment> b)
  {
    return b.GetResult().GetValue();
  }

  public static Func<Zen<RouteEnvironment>, Zen<bool>> ResultValueImplies(Func<Zen<RouteEnvironment>, Zen<bool>> f)
  {
    return b => Zen.Implies(b.GetResultValue(), f(b));
  }

  public static Zen<RouteEnvironment> WithResultValue(this Zen<RouteEnvironment> b, Zen<bool> value)
  {
    return b.WithResult(b.GetResult().WithValue(value));
  }

  public static Zen<RouteEnvironment> WithResultReturned(this Zen<RouteEnvironment> b, Zen<bool> returned)
  {
    return b.WithResult(b.GetResult().WithReturned(returned));
  }

  /// <summary>
  /// Return the route with returned and value both set to true.
  /// </summary>
  /// <param name="r"></param>
  /// <returns></returns>
  public static Zen<RouteEnvironment> ReturnAccept(Zen<RouteEnvironment> r) =>
    r.WithResultReturned(true).WithResultValue(true);
}

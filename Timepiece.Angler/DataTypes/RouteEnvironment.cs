using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.DataTypes;

/// <summary>
///   A representation of a route and the current routing environment:
///   the results of executing the policy, and the current local default action.
/// </summary>
[ZenObject]
public class RouteEnvironment
{
  public const uint DefaultLp = 100;
  public const uint DefaultWeight = 32768;
  public const uint DefaultMetric = 0;
  public static readonly UInt<_2> IncompleteOrigin = new(0);
  public static readonly UInt<_2> ExternalOrigin = new(2);
  public static readonly UInt<_2> InternalOrigin = new(3);

  public RouteEnvironment()
  {
    Prefix = new Ipv4Prefix();
    Weight = DefaultWeight;
    Lp = DefaultLp;
    AsSet = new CSet<string>();
    AsPathLength = 0;
    Metric = DefaultMetric;
    Tag = 0;
    OriginType = IncompleteOrigin;
    Communities = new CSet<string>();
    Result = new RouteResult();
    LocalDefaultAction = false;
    VisitedTerms = new CSet<string>();
  }


  [JsonConstructor]
  public RouteEnvironment(Ipv4Prefix prefix, uint weight, uint lp, CSet<string> asSet, BigInteger asPathLength,
    uint metric, uint tag,
    UInt<_2> originType, CSet<string> communities, RouteResult result,
    bool localDefaultAction, CSet<string> visitedTerms)
  {
    Prefix = prefix;
    Weight = weight;
    Lp = lp;
    AsSet = asSet;
    AsPathLength = asPathLength;
    Metric = metric;
    Tag = tag;
    OriginType = originType;
    Communities = communities;
    Result = result;
    LocalDefaultAction = localDefaultAction;
    VisitedTerms = visitedTerms;
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
  ///   Set representation of the ASes in the AS path.
  /// </summary>
  public CSet<string> AsSet { get; set; }

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

  /// <summary>
  /// Terms visited during the execution of the environment.
  /// </summary>
  public CSet<string> VisitedTerms { get; set; }

  public override string ToString()
  {
    var properties = typeof(RouteEnvironment).GetProperties();
    var propertiesBuilder = new StringBuilder();
    // add each property to the builder
    foreach (var property in properties)
    {
      if (propertiesBuilder.Length > 0) propertiesBuilder.Append(", ");

      propertiesBuilder.Append($"{property.Name}=");
      switch (property.PropertyType.Name)
      {
        case "CSet":
          var value = (CSet<string>?) property.GetValue(this);
          if (value is null)
            propertiesBuilder.Append($"{value}");
          else
            propertiesBuilder.AppendJoin(", ", value.Map.Values.Keys);
          break;
        default:
          propertiesBuilder.Append($"{property.GetValue(this)}");
          break;
      }
    }

    return $"RouteEnvironment({propertiesBuilder})";
  }

  /// <summary>
  ///   Return a Zen boolean encoding that lifts a predicate <paramref name="f"/> over a route
  ///   such that it only applies if the route has a result with a value.
  /// </summary>
  /// <param name="f"></param>
  /// <returns></returns>
  public static Func<Zen<RouteEnvironment>, Zen<bool>> IfValue(Func<Zen<RouteEnvironment>, Zen<bool>> f) =>
    b => Zen.Implies(b.GetResultValue(), f(b));
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

  public static Zen<bool> GetResultValue(this Zen<RouteEnvironment> b) => b.GetResult().GetValue();
  public static Zen<bool> GetResultExit(this Zen<RouteEnvironment> b) => b.GetResult().GetExit();
  public static Zen<bool> GetResultReturned(this Zen<RouteEnvironment> b) => b.GetResult().GetReturned();
  public static Zen<bool> GetResultFallthrough(this Zen<RouteEnvironment> b) => b.GetResult().GetFallthrough();

  /// <summary>
  /// Return true if the RouteEnvironment's Result does not indicate that execution has terminated.
  /// </summary>
  /// <param name="b"></param>
  /// <returns></returns>
  public static Zen<bool> NonTerminated(this Zen<RouteEnvironment> b) => Zen.Not(b.GetResult().IsTerminated());

  public static Zen<RouteEnvironment> WithResultValue(this Zen<RouteEnvironment> b, Zen<bool> value) =>
    b.WithResult(b.GetResult().WithValue(value));

  public static Zen<RouteEnvironment> WithResultReturned(this Zen<RouteEnvironment> b, Zen<bool> returned) =>
    b.WithResult(b.GetResult().WithReturned(returned));

  public static Zen<RouteEnvironment> WithResultFallthrough(this Zen<RouteEnvironment> b, Zen<bool> returned) =>
    b.WithResult(b.GetResult().WithFallthrough(returned));

  /// <summary>
  ///   Return the route with returned and value both set to true.
  /// </summary>
  /// <param name="r"></param>
  /// <returns></returns>
  public static Zen<RouteEnvironment> ReturnAccept(this Zen<RouteEnvironment> r)
  {
    return r.WithResultReturned(true).WithResultValue(true);
  }

  /// <summary>
  /// Reset all the control flow fields of the route's result (everything except for the value).
  /// </summary>
  /// <param name="r"></param>
  /// <returns></returns>
  public static Zen<RouteEnvironment> ResetResultControlFlow(this Zen<RouteEnvironment> r) =>
    r.WithResult(Zen.Constant(new RouteResult()).WithValue(r.GetResultValue()));

  /// <summary>
  /// Add the given term string to the route's visited terms.
  /// </summary>
  /// <param name="route"></param>
  /// <param name="term"></param>
  /// <returns></returns>
  public static Zen<RouteEnvironment> AddVisitedTerm(this Zen<RouteEnvironment> route, string term) =>
    route.WithVisitedTerms(route.GetVisitedTerms().Add(term));
}

using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler;

public static class Internet2
{
  /// <summary>
  ///   The block to external tag used by Internet2.
  /// </summary>
  private const string Bte = "11537:888";

  public static readonly string[] Internet2Nodes =
    {"atla-re1", "chic", "clev-re1", "hous", "kans-re1", "losa", "newy-re1", "salt-re1", "seat-re1", "wash"};

  /// <summary>
  ///   Addresses for neighbors in the OTHER-INTERNAL peer group of the internal nodes.
  ///   These connections should also be considered internal.
  /// </summary>
  public static readonly string[] OtherInternalNodes =
  {
    "64.57.16.133", "64.57.16.196", "64.57.16.4", "64.57.16.68", "64.57.17.133", "64.57.17.194",
    "64.57.17.7", "64.57.17.71", "64.57.19.2"
  };

  public static readonly IEnumerable<string> InternalNodes = Internet2Nodes.Concat(OtherInternalNodes);

  /// <summary>
  ///   A prefix corresponding to the internal nodes of Internet2.
  /// </summary>
  public static readonly Ipv4Prefix InternalPrefix = new("64.57.28.0", "64.57.28.255");

  /// <summary>
  ///   Predicate that the route is for the internal prefix.
  /// </summary>
  /// <param name="env"></param>
  /// <returns></returns>
  public static Zen<bool> HasInternalRoute(Zen<RouteEnvironment> env)
  {
    return Zen.And(env.GetResultValue(), env.GetPrefix() == InternalPrefix);
  }

  /// <summary>
  ///   Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  public static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env)
  {
    return Zen.Implies(env.GetResultValue(), Zen.Not(env.GetCommunities().Contains(Bte)));
  }
}

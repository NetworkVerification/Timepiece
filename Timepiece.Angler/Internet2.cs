using System.Numerics;
using Timepiece.Angler.UntypedAst;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Datatypes;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler;

using EnvNet = Network<RouteEnvironment, RouteEnvironment>;

public class Internet2 : RouteEnvironmentAst
{
  public static readonly string[] InternalNodes =
    {"atla-re1", "chic", "clev-re1", "hous", "kans-re1", "losa", "newy-re1", "salt-re1", "seat-re1", "wash"};

  /// <summary>
  /// A prefix corresponding to the internal nodes of Internet2.
  /// </summary>
  public static readonly Ipv4Prefix InternalPrefix = new("64.57.28.0", "64.57.28.255");

  /// <summary>
  /// Predicate that the route is for the internal prefix.
  /// </summary>
  /// <param name="env"></param>
  /// <returns></returns>
  public static Zen<bool> HasInternalRoute(Zen<RouteEnvironment> env) =>
    Zen.And(env.GetResultValue(), env.GetPrefix() == InternalPrefix);

  public Internet2(Dictionary<string, NodeProperties> nodes, Ipv4Prefix? destination,
    Dictionary<string, AstPredicate> predicates, Dictionary<string, string?> symbolics, BigInteger? convergeTime) :
    base(nodes, destination, predicates, symbolics, convergeTime)
  {
  }


  /// <summary>
  /// The block to external tag used by Internet2.
  /// </summary>
  private const string Bte = "11537:888";

  /// <summary>
  /// Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  public static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env) =>
    Zen.Implies(env.GetResultValue(), Zen.Not(env.GetCommunities().Contains(Bte)));

  /// <summary>
  /// Extract the network from the BlockToExternal class.
  /// </summary>
  /// <param name="f">A function that may arbitrarily modify the constructed network.</param>
  /// <returns></returns>
  public EnvNet ToNetwork(Func<EnvNet, EnvNet> f)
  {
    var net = base.ToNetwork();
    return f(net);
  }
}

using System.Numerics;
using NetTools;
using Timepiece.Angler.UntypedAst;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Datatypes;
using Xunit;
using ZenLib;

namespace Timepiece.Angler;

using Route = Pair<bool, BatfishBgpRoute>;

public static class AstTests
{
  private const string IsValid = "IsValid";
  private static readonly Ipv4Prefix D = new("70.0.19.1");

  private static readonly Dictionary<string, AstPredicate<Route>> Predicates = new()
  {
    {IsValid, PairRouteAst.IsValid}
  };

  private static readonly PairRouteAst SpAst = GenerateSpAst(4, FatTree.FatTreeLayer.Edge.Node(19));

  private static NodeProperties<Route> GenerateProperties(string node,
    IEnumerable<string> neighbors, BigInteger time)
  {
    // no policies defined for any edge
    var policies = new Dictionary<string, RoutingPolicies>(neighbors.Select(nbr =>
      new KeyValuePair<string, RoutingPolicies>(nbr, new RoutingPolicies())));

    // determine the node's initial value
    Expr initial;
    if (GetAddressRange(node).Contains(D))
    {
      initial = new PairExpr(new BoolExpr(true), new CreateRecord(typeof(BatfishBgpRoute),
        new Dictionary<string, Expr>
        {
          {"Prefix", new PrefixExpr(D)},
          {"AdminDist", new UIntExpr(0U)},
          {"Lp", new UIntExpr(0U)},
          {"AsPathLength", new BigIntExpr(BigInteger.Zero)},
          {"Med", new UIntExpr(0U)},
          {"OriginType", new UInt2Expr(new UInt<_2>(0))},
          {"Communities", new LiteralSet(new dynamic[] { })},
        }));
    }
    else
    {
      initial = new PairExpr(new BoolExpr(false), new CreateRecord(typeof(BatfishBgpRoute),
        new Dictionary<string, Expr>
        {
          {"Prefix", new PrefixExpr(new Ipv4Prefix())},
          {"AdminDist", new UIntExpr(0U)},
          {"Lp", new UIntExpr(0U)},
          {"AsPathLength", new BigIntExpr(BigInteger.Zero)},
          {"Med", new UIntExpr(0U)},
          {"OriginType", new UInt2Expr(new UInt<_2>(0))},
          {"Communities", new LiteralSet(new dynamic[] { })},
        }));
    }

    return new NodeProperties<Route>(
      policies, IsValid, new Finally<Route>(time, IsValid), new Dictionary<string, AstFunction<Route>>(),
      initial);
  }

  private static IPAddressRange GetAddressRange(string node)
  {
    var index = int.Parse(node[(node.IndexOf('-') + 1)..]);
    return IPAddressRange.Parse($"70.0.{index}.0/24");
  }

  private static PairRouteAst GenerateSpAst(uint numPods, string destNode)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destNode);
    var props = topology.MapNodes(n =>
      GenerateProperties(n, topology[n], distances[n]));
    return new PairRouteAst(props, D, Predicates, new Dictionary<string, AstPredicate<Unit>>(), 5);
  }

  [Fact]
  public static void TestSpAstGoodAnnotations()
  {
    Assert.False(SpAst.ToNetwork().CheckAnnotations().HasValue);
  }

  [Fact]
  public static void TestSpAstGoodMonolithic()
  {
    Assert.False(SpAst.ToNetwork().CheckMonolithic().HasValue);
  }

  [Fact]
  public static void TestSpAstBadAnnotations()
  {
    var badSp = SpAst;
    badSp.Nodes[FatTree.FatTreeLayer.Edge.Node(19)].Temporal = new Finally<Route>(5, IsValid);
    Assert.True(badSp.ToNetwork().CheckInductive().HasValue);
  }

  [Fact]
  public static void TestSpAstBadMonolithic()
  {
    var badSp = SpAst;
    badSp.Predicates[IsValid] = new AstPredicate<Route>("route", new BoolExpr(false));
    Assert.True(badSp.ToNetwork().CheckMonolithic().HasValue);
  }
}

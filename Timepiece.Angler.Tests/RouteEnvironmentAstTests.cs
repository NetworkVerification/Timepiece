using System.Numerics;
using NetTools;
using Timepiece.Angler.Ast;
using Timepiece.Datatypes;

namespace Timepiece.Angler.Tests;

public static class AstTests
{
  private const string IsValid = "IsValid";
  private static readonly Ipv4Prefix D = new("70.0.19.1");

  private static readonly Dictionary<string, AstPredicate> Predicates = new()
  {
    {IsValid, RouteEnvironmentAst.IsValid}
  };

  private static readonly RouteEnvironmentAst SpAst = GenerateSpAst(4, FatTree.FatTreeLayer.Edge.Node(19));

  private static NodeProperties GenerateProperties(string node,
    IEnumerable<string> neighbors, BigInteger time)
  {
    // no policies defined for any edge
    var policies = new Dictionary<string, RoutingPolicies>(neighbors.Select(nbr =>
      new KeyValuePair<string, RoutingPolicies>(nbr, new RoutingPolicies())));

    // determine the node's initial value
    Expr initial;
    if (GetAddressRange(node).Contains(D))
      initial = new WithField(
        new WithField(AstEnvironment.DefaultRoute(), "Prefix", new PrefixExpr(D)),
        "Value", new BoolExpr(true));
    else
      initial = AstEnvironment.DefaultRoute();

    return new NodeProperties(null,
      policies, IsValid, new Finally(time, IsValid),
      new Dictionary<string, AstFunction<RouteEnvironment>>(),
      initial);
  }

  private static IPAddressRange GetAddressRange(string node)
  {
    var index = int.Parse(node[(node.IndexOf('-') + 1)..]);
    return IPAddressRange.Parse($"70.0.{index}.0/24");
  }

  private static RouteEnvironmentAst GenerateSpAst(uint numPods, string destNode)
  {
    var topology = Topologies.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destNode);
    var props = topology.MapNodes(n =>
      GenerateProperties(n, topology[n], distances[n]));
    return new RouteEnvironmentAst(props, D, Predicates, new Dictionary<string, string?>(), 5);
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
    badSp.Nodes[FatTree.FatTreeLayer.Edge.Node(19)].Temporal = new Finally(5, IsValid);
    Assert.True(badSp.ToNetwork().CheckInductive().HasValue);
  }

  [Fact]
  public static void TestSpAstBadMonolithic()
  {
    var badSp = SpAst;
    badSp.Predicates[IsValid] = new AstPredicate("route", new BoolExpr(false));
    Assert.True((bool) badSp.ToNetwork().CheckMonolithic().HasValue);
  }
}

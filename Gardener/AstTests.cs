using System.Numerics;
using Gardener.AstFunction;
using Karesansui;
using NetTools;
using Newtonsoft.Json.Linq;
using Xunit;
using ZenLib;

namespace Gardener;

using Route = Pair<bool, BatfishBgpRoute>;

public static class AstTests
{
  private static readonly Destination D = new("70.0.19.1");

  private const string IsValid = "IsValid";

  private static readonly Dictionary<string, AstPredicate<Route>> Predicates = new()
  {
    {IsValid, PairRouteAst.IsValid}
  };

  private static NodeProperties<Route> GenerateProperties(string node,
    IEnumerable<string> neighbors, BigInteger time)
  {
    var policies = new Dictionary<string, RoutingPolicies>(neighbors.Select(nbr =>
      new KeyValuePair<string, RoutingPolicies>(nbr, new RoutingPolicies())));
    return new NodeProperties<Route>(new List<IPAddressRange>
      {
        GetAddressRange(node)
      }, policies, IsValid, new Finally<Route>(time, IsValid), new Dictionary<string, AstFunction<Route>>(),
      new Dictionary<string, JObject>());
  }

  private static IPAddressRange GetAddressRange(string node)
  {
    var index = int.Parse(node[(node.IndexOf('-') + 1)..]);
    return IPAddressRange.Parse($"70.0.{index}.0/24");
  }

  private static readonly PairRouteAst SpAst = GenerateSpAst(4, "edge-19");

  private static PairRouteAst GenerateSpAst(int numPods, string destNode)
  {
    var topology = Default.FatTree(numPods);
    var distances = topology.BreadthFirstSearch(destNode);
    var props = topology.ForAllNodes(n =>
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
    badSp.Nodes["edge-19"].Temporal = new Finally<Route>(5, IsValid);
    Assert.True(badSp.ToNetwork().CheckInductive().HasValue);
  }
}

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

  private static readonly Dictionary<string, AstPredicate<Route>> Predicates = new()
  {
    {"IsValid", PairRouteAst.IsValid}
  };

  private static NodeProperties<Route> GenerateProperties(string node,
    IEnumerable<string> neighbors, BigInteger time)
  {
    var policies = new Dictionary<string, RoutingPolicies>(neighbors.Select(nbr =>
      new KeyValuePair<string, RoutingPolicies>(nbr, new RoutingPolicies())));
    return new NodeProperties<Route>(new List<IPAddressRange>
      {
        GetAddressRange(node)
      }, policies, "IsValid", new Finally<Route>(time, "IsValid"), new Dictionary<string, AstFunction<Route>>(),
      new Dictionary<string, JObject>());
  }

  private static IPAddressRange GetAddressRange(string node)
  {
    var index = int.Parse(node[(node.IndexOf('-') + 1)..]);
    return IPAddressRange.Parse($"70.0.{index}.0/24");
  }

  [Fact]
  public static void TestSpAstGoodAnnotations()
  {
    var topology = Default.FatTree(4);
    const string dest = "edge-19";
    var props = topology.ForAllNodes(n =>
      GenerateProperties(n, topology[n], topology.BreadthFirstSearch(dest, n)));
    var ast = new PairRouteAst(props, D, Predicates, new Dictionary<string, AstPredicate<Unit>>(), 5);
    Assert.False(ast.ToNetwork().CheckAnnotations().HasValue);
  }
}

using System.Numerics;
using Gardener.AstFunction;
using Karesansui;
using NetTools;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Gardener;

using Route = ZenLib.Pair<bool, BatfishBgpRoute>;

public static class AstTests
{
  private static readonly Destination D = new("70.0.19.1");

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

  private static BigInteger BreadthFirstSearch(Topology topology, string start, string goal)
  {
    var q = new Queue<string>();
    var visited = new Dictionary<string, BigInteger>
    {
      {start, 0}
    };
    q.Enqueue(start);
    while (q.Count > 0)
    {
      var n = q.Dequeue();
      var d = visited[n];
      if (goal == n)
      {
        return d;
      }

      foreach (var m in topology[n].Where(m => !visited.ContainsKey(m)))
      {
        visited.Add(m, d + 1);
        q.Enqueue(m);
      }
    }

    return BigInteger.MinusOne;
  }

  [Fact]
  public static void TestSpAstGoodAnnotations()
  {
    var topology = Default.FatTree(4);
    const string dest = "edge-19";
    var props = topology.ForAllNodes(n => GenerateProperties(n, topology[n], BreadthFirstSearch(topology, dest, n)));
    var ast = new PairRouteAst(props, D);
    Assert.False(ast.ToNetwork().CheckAnnotations().HasValue);
  }
}

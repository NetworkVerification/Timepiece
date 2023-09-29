using System.Numerics;
using NetTools;
using Timepiece.Angler.Ast;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Tests;

public static class AstTests
{
  private static readonly Ipv4Prefix D = new("70.0.19.1");
  private static readonly string DestinationNode = FatTree.FatTreeLayer.Edge.Node(19);

  private static readonly AnglerNetwork SpAnglerNetwork = GenerateSpAst(4);

  private static NodeProperties GenerateProperties(IEnumerable<string> neighbors)
  {
    // no policies defined for any edge
    var policies = new Dictionary<string, RoutingPolicies>(neighbors.Select(nbr =>
      new KeyValuePair<string, RoutingPolicies>(nbr, new RoutingPolicies())));

    return new NodeProperties(null, policies, new Dictionary<string, AstFunction<RouteEnvironment>>(),
      new List<Ipv4Prefix>());
  }

  private static NetworkQuery<RouteEnvironment, string> IsValidQuery(Digraph<string> graph,
    string destNode)
  {
    var distances = graph.BreadthFirstSearch(destNode);
    var initialRoutes = graph.MapNodes(n => GetAddressRange(n).Contains(D)
      ? Zen.Constant(new RouteEnvironment()).WithPrefix(D).WithResultValue(true)
      : Zen.Constant(new RouteEnvironment()));
    var monolithicProperties =
      graph.MapNodes(_ => new Func<Zen<RouteEnvironment>, Zen<bool>>(env => env.GetResult().GetValue()));
    var annotations = graph.MapNodes(n => Lang.Finally(distances[n], monolithicProperties[n]));
    var modularProperties = graph.MapNodes(n => Lang.Finally(new BigInteger(4), monolithicProperties[n]));
    return new NetworkQuery<RouteEnvironment, string>(initialRoutes,
      System.Array.Empty<ISymbolic>(), monolithicProperties, modularProperties, annotations);
  }

  private static IPAddressRange GetAddressRange(string node)
  {
    var index = int.Parse(node[(node.IndexOf('-') + 1)..]);
    return IPAddressRange.Parse($"70.0.{index}.0/24");
  }

  private static AnglerNetwork GenerateSpAst(uint numPods)
  {
    var topology = Topologies.FatTree(numPods);
    var props = topology.MapNodes(n =>
      GenerateProperties(topology[n]));
    return new AnglerNetwork(props, new List<ExternalPeer>());
  }

  [Fact]
  public static void TestSpAstGoodAnnotations()
  {
    var (topology, transfer) = SpAnglerNetwork.TopologyAndTransfer();
    var query = IsValidQuery(topology, DestinationNode);
    Assert.False(
      query.ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional).CheckAnnotations().HasValue);
  }

  [Fact]
  public static void TestSpAstGoodMonolithic()
  {
    var (topology, transfer) = SpAnglerNetwork.TopologyAndTransfer();
    var query = IsValidQuery(topology, DestinationNode);
    Assert.False(query.ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional).CheckMonolithic()
      .HasValue);
  }

  [Fact]
  public static void TestSpAstBadAnnotations()
  {
    var (topology, transfer) = SpAnglerNetwork.TopologyAndTransfer();
    var query = IsValidQuery(topology, DestinationNode);
    query.Annotations[FatTree.FatTreeLayer.Edge.Node(18)] =
      Lang.Finally<RouteEnvironment>(new BigInteger(1), _ => Zen.False());
    Assert.True(query.ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional).CheckInductive().HasValue);
  }

  [Fact]
  public static void TestSpAstBadMonolithic()
  {
    var (topology, transfer) = SpAnglerNetwork.TopologyAndTransfer();
    var query = IsValidQuery(topology, DestinationNode);
    query.MonolithicProperties[FatTree.FatTreeLayer.Edge.Node(19)] = _ => Zen.False();
    Assert.True(query.ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional).CheckMonolithic().HasValue);
  }
}

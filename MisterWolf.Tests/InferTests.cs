using System.Numerics;
using Timepiece;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace MisterWolf.Tests;

public static class InferTests
{
  // a boolean network where one node has a route initially and others do not
  private static Network<bool, TV, Unit> BoolNet<TV>(Topology<TV> topology, TV destination) where TV : IEquatable<TV> =>
    new BooleanNetwork<TV, Unit>(topology,
      topology.MapNodes(n => n.Equals(destination) ? Zen.True() : Zen.False()), Array.Empty<SymbolicValue<Unit>>());

  // a boolean network where one symbolically-chosen node has a route initially and others do not
  private static Network<bool, TV, TV> BoolNetMultiDest<TV>(Topology<TV> topology) where TV : notnull
  {
    var dest = new SymbolicValue<TV>("dest",
      x => topology.Nodes.Aggregate(Zen.False(), (b, n) => Zen.Or(b, x == n)));
    return new BooleanNetwork<TV, TV>(topology,
      topology.MapNodes(n => dest.EqualsValue(n)), new[] {dest});
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="fatTree"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  private static Dictionary<string, uint> FatTreeDistances(LabelledTopology<string, int> fatTree, string destination)
  {
    return fatTree.MapNodes(n =>
    {
      if (n == destination) return 0U;
      if (n.IsAggregation() && fatTree.L(destination) == fatTree.L(n)) return 1U;
      if (n.IsAggregation() && fatTree.L(destination) != fatTree.L(n)) return 3U;
      if (n.IsEdge() && fatTree.L(destination) != fatTree.L(n)) return 4U;
      return 2U;
    });
  }

  private static readonly int[] PathSizes = {3, 4, 5, 20};

  private static readonly Func<Zen<bool>, Zen<bool>>[] BooleanBeforeInvariantPredicates =
  {
    Lang.Not(Lang.Identity<bool>()),
    Lang.True<bool>()
  };

  public static CartesianTheoryData<int, Func<Zen<bool>, Zen<bool>>> cartesianData =
    new(PathSizes, BooleanBeforeInvariantPredicates);

  [Theory]
  [MemberData(nameof(cartesianData))]
  public static void CheckBoolPathInferSucceeds(uint numNodes, Func<Zen<bool>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.Path(numNodes, false);
    var infer = new Infer<bool, string, Unit>(BoolNet(topology, "0"),
      topology.MapNodes(_ => beforePredicate),
      topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimesSymbolic();
    Assert.True(times.Count > 0, "Failed to infer times.");
    foreach (var (node, time) in times) Assert.True(time >= int.Parse(node));
  }

  public static CartesianTheoryData<int, Func<Zen<bool>, Zen<bool>>> cartesianFatTreeData =
    new(new[] {4}, BooleanBeforeInvariantPredicates);

  [Theory]
  [MemberData(nameof(cartesianFatTreeData))]
  public static void CheckBoolFatTreeInferSucceeds(uint numPods, Func<Zen<bool>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var destination = FatTree.FatTreeLayer.Edge.Node((uint) (Math.Pow(numPods, 2) * 1.25 - 1));
    var distances = FatTreeDistances(topology, destination);
    var infer = new Infer<bool, string, Unit>(BoolNet(topology, destination),
      topology.MapNodes(_ => beforePredicate),
      topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimesSymbolic();
    Assert.True(times.Count > 0, "Failed to infer times.");
    foreach (var (node, time) in times) Assert.True(time >= distances[node]);
  }

  [Theory]
  [InlineData(2)]
  public static void CheckBoolPathMultiDestInferFails(uint numNodes)
  {
    var topology = Topologies.Path(numNodes, false);
    var infer = new Infer<bool, string, string>(BoolNetMultiDest(topology),
      topology.MapNodes(_ => Lang.Const(true)), topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimesExplicit();
    Assert.True(times.Count == 0, "Time inference should fail for multi-destination.");
  }

  private static readonly Func<Zen<Option<BigInteger>>, Zen<bool>>[] OptionUintBeforeInvariantPredicates =
  {
    Lang.IsNone<BigInteger>(),
    Lang.True<Option<BigInteger>>()
  };

  public static CartesianTheoryData<int, Func<Zen<Option<BigInteger>>, Zen<bool>>> optionIntCartesianData =
    new(PathSizes, OptionUintBeforeInvariantPredicates);

  [Theory]
  [MemberData(nameof(optionIntCartesianData))]
  public static void CheckOptionUintPathInferSucceeds(uint numNodes,
    Func<Zen<Option<BigInteger>>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.Path(numNodes, false);
    var net = new ShortestPath<string, Unit>(topology, "0", Array.Empty<SymbolicValue<Unit>>());
    var beforeInvariants = topology.MapNodes(_ => beforePredicate);
    // eventually, the route must be less than the specified max
    var afterInvariants = topology.MapNodes(n => Lang.IfSome<BigInteger>(x => x <= BigInteger.Parse(n)));
    var infer = new Infer<Option<BigInteger>, string, Unit>(net, beforeInvariants, afterInvariants);
    var times = infer.InferTimesSymbolic();
    Assert.True(times.Count > 0, "Failed to infer times.");
    foreach (var (node, time) in times) Assert.True(time >= int.Parse(node));
  }
}

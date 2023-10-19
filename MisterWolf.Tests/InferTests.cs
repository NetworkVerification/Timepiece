using System.Numerics;
using Timepiece;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace MisterWolf.Tests;

public static class InferTests
{
  private static readonly int[] PathSizes = {3, 4, 5, 20};

  private static readonly Func<Zen<bool>, Zen<bool>>[] BooleanBeforeInvariantPredicates =
  {
    Lang.Not(Lang.Identity<bool>()),
    Lang.True<bool>()
  };

  public static CartesianTheoryData<InferenceStrategy, int, Func<Zen<bool>, Zen<bool>>> cartesianData =
    new(Strategies, PathSizes, BooleanBeforeInvariantPredicates);

  public static CartesianTheoryData<InferenceStrategy, int, Func<Zen<bool>, Zen<bool>>> cartesianFatTreeData =
    new(Strategies, new[] {4}, BooleanBeforeInvariantPredicates);

  private static readonly Func<Zen<Option<BigInteger>>, Zen<bool>>[] OptionUintBeforeInvariantPredicates =
  {
    Lang.IsNone<BigInteger>(),
    Lang.True<Option<BigInteger>>()
  };

  public static CartesianTheoryData<int, Func<Zen<Option<BigInteger>>, Zen<bool>>> optionIntCartesianData =
    new(PathSizes, OptionUintBeforeInvariantPredicates);

  private static IEnumerable<InferenceStrategy> Strategies => new[]
  {
    InferenceStrategy.ExplicitEnumeration,
    InferenceStrategy.SymbolicEnumeration,
    InferenceStrategy.SelectiveEnumeration
  };

  public static TheoryData<Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>> TriangleNetBeforeInvariants =>
    new()
    {
      // allowing any route before convergence
      new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>
      {
        {"A", s => s == Option.Some(new BigInteger(0))},
        // is a nonnegative integer or "infinity" (none)
        {"B", s => s.Where(x => x < BigInteger.Zero).IsNone()},
        {"C", s => s.Where(x => x < BigInteger.Zero).IsNone()}
      },
      // allowing exact routes before convergence
      new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>
      {
        {"A", s => s == Option.Some(new BigInteger(0))},
        {"B", s => s == Option.None<BigInteger>()},
        {"C", s => Zen.Or(s == Option.None<BigInteger>(), s == Option.Some(new BigInteger(3)))}
      }
    };

  // a boolean network where one node has a route initially and others do not
  private static Network<bool, TV> BoolNet<TV>(Digraph<TV> digraph, TV destination) where TV : IEquatable<TV>
  {
    return new BooleanNetwork<TV>(digraph,
      digraph.MapNodes(n => n.Equals(destination) ? Zen.True() : Zen.False()), Array.Empty<ISymbolic>());
  }

  // a boolean network where one symbolically-chosen node has a route initially and others do not
  private static Network<bool, TV> BoolNetMultiDest<TV>(Digraph<TV> digraph) where TV : notnull
  {
    var dest = new SymbolicValue<TV>("dest",
      x => digraph.Nodes.Aggregate(Zen.False(), (b, n) => Zen.Or(b, x == n)));
    return new BooleanNetwork<TV>(digraph,
      digraph.MapNodes(n => dest.EqualsValue(n)), new ISymbolic[] {dest});
  }

  // a triangular integer network where the path A-B-C is cheaper than the path A-C
  private static Network<Option<BigInteger>, string> TriangleNet()
  {
    var topology = Topologies.Complete(3, true);
    var transfer =
      topology.MapEdges<Func<Zen<Option<BigInteger>>, Zen<Option<BigInteger>>>>(e =>
        e.Item1 == "B" || e.Item2 == "B"
          ? Lang.Omap<BigInteger, BigInteger>(x => x + BigInteger.One)
          : Lang.Omap<BigInteger, BigInteger>(x => x + new BigInteger(3)));
    return new Network<Option<BigInteger>, string>(topology, transfer, Lang.Omap2<BigInteger>(Zen.Min),
      topology.MapNodes<Zen<Option<BigInteger>>>(n =>
        n == "A" ? Option.Create<BigInteger>(BigInteger.Zero) : Option.Null<BigInteger>()),
      Array.Empty<ISymbolic>());
  }

  /// <summary>
  /// </summary>
  /// <param name="fatTree"></param>
  /// <param name="destination"></param>
  /// <returns></returns>
  private static Dictionary<string, uint> FatTreeDistances(NodeLabelledDigraph<string, int> fatTree, string destination)
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

  [Theory]
  [MemberData(nameof(cartesianData))]
  public static void CheckBoolPathInferSucceeds(InferenceStrategy strategy, uint numNodes,
    Func<Zen<bool>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.Path(numNodes, false);
    var infer = new Infer<bool, string>(BoolNet(topology, "0"),
      topology.MapNodes(_ => beforePredicate),
      topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimes(strategy);
    Assert.True(times.Count > 0, "Failed to infer times.");
    foreach (var (node, time) in times)
      Assert.True(time >= int.Parse(node), $"Time {time} did not match expected time {int.Parse(node)}");
  }

  [Theory]
  [MemberData(nameof(cartesianFatTreeData))]
  public static void CheckBoolFatTreeInferSucceeds(InferenceStrategy strategy, uint numPods,
    Func<Zen<bool>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.LabelledFatTree(numPods);
    var destination = FatTree.FatTreeLayer.Edge.Node((uint) (Math.Pow(numPods, 2) * 1.25 - 1));
    var distances = FatTreeDistances(topology, destination);
    var infer = new Infer<bool, string>(BoolNet(topology, destination),
      topology.MapNodes(_ => beforePredicate),
      topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimes(strategy);
    Assert.True(times.Count > 0, "Failed to infer times.");
    foreach (var (node, time) in times)
      Assert.True(time >= distances[node], $"Time {time} did not match expected time {distances[node]}");
  }

  [Theory]
  [InlineData(2)]
  public static void CheckBoolPathMultiDestInferFails(uint numNodes)
  {
    var topology = Topologies.Path(numNodes, false);
    var infer = new Infer<bool, string>(BoolNetMultiDest(topology),
      topology.MapNodes(_ => Lang.Const(true)), topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimes(InferenceStrategy.SymbolicEnumeration);
    Assert.True(times.Count == 0, "Time inference should fail for multi-destination.");
  }

  [Theory]
  [MemberData(nameof(optionIntCartesianData))]
  public static void CheckOptionUintPathInferSucceeds(uint numNodes,
    Func<Zen<Option<BigInteger>>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.Path(numNodes, false);
    var net = new ShortestPath<string>(topology, "0", Array.Empty<ISymbolic>());
    var beforeInvariants = topology.MapNodes(_ => beforePredicate);
    // eventually, the route must be less than the specified max
    var afterInvariants = topology.MapNodes(n => Lang.IfSome<BigInteger>(x => x <= BigInteger.Parse(n)));
    var infer = new Infer<Option<BigInteger>, string>(net, beforeInvariants, afterInvariants);
    var times = infer.InferTimes(InferenceStrategy.SymbolicEnumeration);
    Assert.True(times.Count > 0, "Failed to infer times.");
    foreach (var (node, time) in times) Assert.True(time >= int.Parse(node));
  }

  [Theory]
  [MemberData(nameof(TriangleNetBeforeInvariants))]
  public static void CheckTriangleNetInferSucceeds(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>> beforeInvariants)
  {
    var net = TriangleNet();
    var afterInvariants = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>
    {
      {"A", s => s == Option.Some(new BigInteger(0))},
      {"B", s => s == Option.Some(new BigInteger(1))},
      {"C", s => s == Option.Some(new BigInteger(2))}
    };
    var infer = new Infer<Option<BigInteger>, string>(net, beforeInvariants, afterInvariants)
    {
      // fix the maximum time at 2
      MaxTime = 2
    };
    var times = infer.InferTimes(InferenceStrategy.ExplicitEnumeration);
    // check that all times are correct
    Assert.True(times.Count > 0, "Failed to infer times.");
    Assert.Equal(1, times["B"]);
    Assert.Equal(2, times["C"]);
    // confirm that all checks pass
    var annotations = net.Digraph.MapNodes(n => Lang.Until(times[n], beforeInvariants[n], afterInvariants[n]));
    var annotated = new AnnotatedNetwork<Option<BigInteger>, string>(net, annotations,
      net.Digraph.MapNodes(n => Lang.Finally(new BigInteger(2), Lang.IsSome<BigInteger>())),
      net.Digraph.MapNodes(_ => Lang.IsSome<BigInteger>()));
    Assert.Equal(Option.None<State<Option<BigInteger>, string>>(), annotated.CheckAnnotations());
  }
}

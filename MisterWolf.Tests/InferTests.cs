using System.Diagnostics.Contracts;
using Timepiece;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace MisterWolf.Tests;

/// <summary>
/// A TheoryData subclass for generating tests that take the Cartesian product of two enumerables.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public class CartesianTheoryData<T1, T2> : TheoryData<T1, T2>
{
  public CartesianTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2)
  {
    Contract.Assert(data1 != null && data1.Any());
    Contract.Assert(data2 != null && data2.Any());

    var enumerable = data2 as T2[] ?? data2.ToArray();
    foreach (var t1 in data1)
    foreach (var t2 in enumerable)
      Add(t1, t2);
  }
}

public static class InferTests
{
  // a boolean network where one node has a route initially and others do not
  private static Network<bool, Unit> BoolNet(Topology topology) => new BooleanNetwork<Unit>(topology,
    topology.MapNodes(n => n == "0" ? Zen.True() : Zen.False()), Array.Empty<SymbolicValue<Unit>>());

  // a boolean network where one symbolically-chosen node has a route initially and others do not
  private static Network<bool, string> BoolNetMultiDest(Topology topology)
  {
    var dest = new SymbolicValue<string>("dest",
      x => topology.Nodes.Aggregate(Zen.False(), (b, n) => Zen.Or(b, x == n)));
    return new BooleanNetwork<string>(topology,
      topology.MapNodes(n => dest.EqualsValue(n)), new[] {dest});
  }

  private static readonly int[] PathSizes = {3, 4, 5, 20};

  private static readonly Func<Zen<bool>, Zen<bool>>[] BeforeInvariantPredicates =
  {
    Lang.Not(Lang.Identity<bool>()),
    Lang.True<bool>()
  };

  public static CartesianTheoryData<int, Func<Zen<bool>, Zen<bool>>> cartesianData =
    new(PathSizes, BeforeInvariantPredicates);

  [Theory]
  [MemberData(nameof(cartesianData))]
  public static void CheckBoolPathInferSucceeds(uint numNodes, Func<Zen<bool>, Zen<bool>> beforePredicate)
  {
    var topology = Topologies.Path(numNodes, false);
    var infer = new Infer<bool, Unit>(BoolNet(topology),
      topology.MapNodes(_ => beforePredicate),
      topology.MapNodes(_ => Lang.Identity<bool>()))
    {
      PrintBounds = true,
    };
    var times = infer.InferTimesSymbolic();
    Assert.True(times.Count > 0, "Failed to infer times.");
    Assert.True(times.Aggregate(true, (b, n) => b && n.Value >= int.Parse(n.Key)));
  }

  [Theory]
  [InlineData(2)]
  public static void CheckBoolPathMultiDestInferFails(uint numNodes)
  {
    var topology = Topologies.Path(numNodes, false);
    var infer = new Infer<bool, string>(BoolNetMultiDest(topology),
      topology.MapNodes(_ => Lang.Const(true)), topology.MapNodes(_ => Lang.Identity<bool>()));
    var times = infer.InferTimesExplicit();
    Assert.True(times.Count == 0, "Time inference should fail for multi-destination.");
  }
}

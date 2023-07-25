using Xunit;
using ZenLib;

namespace MisterWolf.Tests;

public static class ArrangementTests
{
  public static TheoryData<Option<bool>, IEnumerable<Option<bool>>> ConcreteArrangementEnumerable => new()
  {
    {Option.Some(true), new Option<bool>[] { }},
    {Option.Some(false), new Option<bool>[] { }},
    {Option.None<bool>(), new Option<bool>[] { }},
    {Option.Some(true), new[] {Option.Some(true)}},
    {Option.Some(false), new[] {Option.Some(false)}},
    {Option.None<bool>(), new[] {Option.None<bool>()}}
  };

  public static TheoryData<Option<bool>, FSeq<Option<bool>>> ConcreteArrangementParameters => new()
  {
    {Option.Some(true), new FSeq<Option<bool>>()},
    {Option.Some(false), new FSeq<Option<bool>>()},
    {Option.None<bool>(), new FSeq<Option<bool>>()},
    {Option.Some(true), new FSeq<Option<bool>>().AddFront(Option.Some(true))},
    {Option.Some(false), new FSeq<Option<bool>>().AddFront(Option.Some(false))},
    {Option.None<bool>(), new FSeq<Option<bool>>().AddFront(Option.None<bool>())}
  };

  [Theory]
  [MemberData(nameof(ConcreteArrangementParameters))]
  public static void ConstructValidConcreteArrangements(Option<bool> invariant, FSeq<Option<bool>> neighbors)
  {
    var a = new Arrangement(invariant, neighbors);
    Assert.Equal(invariant, a.Invariant);
    Assert.Equal(neighbors, a.Neighbors);
  }

  [Theory]
  [MemberData(nameof(ConcreteArrangementEnumerable))]
  public static void ConstructValidConcreteArrangementsEnumerable(Option<bool> invariant,
    IEnumerable<Option<bool>> neighbors)
  {
    var n = neighbors.ToArray();
    var a = new Arrangement(invariant, n);
    Assert.Equal(invariant, a.Invariant);
    for (var i = 0; i < n.Length; i++)
    {
      var v = n[i];
      Assert.Equal(v, a[i]);
    }
  }

  [Fact]
  public static void ConstructSymbolicArrangement()
  {
    // var a = ArrangementExtensions.Create(Zen.Symbolic<Option<bool>>(),
    // FSeq.Create(Enumerable.Repeat(Zen.Symbolic<Option<bool>>(), 3)));
    // var a = Zen.Symbolic<Arrangement>();
    var a = ArrangementExtensions.Symbolic(3);
    var query = Zen.And(a.GetInvariant().IsSome(), a.GetNeighbor(2).Case(none: () => false, some: b => b)).Solve();
    Assert.True(query.IsSatisfiable());
    var a2 = query.Get(a);
    Assert.True(a2.Invariant.HasValue);
    Assert.True(a2[2].HasValue && a2[2].Value);
  }
}

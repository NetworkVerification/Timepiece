using Xunit;
using ZenLib;

namespace MisterWolf.Tests;

public static class CMapArrangementTests
{
  public static TheoryData<Option<bool>, IEnumerable<(string, Option<bool>)>> ConcreteArrangementEnumerable => new()
  {
    {Option.Some(true), new (string, Option<bool>)[] { }},
    {Option.Some(false), new (string, Option<bool>)[] { }},
    {Option.None<bool>(), new (string, Option<bool>)[] { }},
    {Option.Some(true), new[] {("A", Option.Some(true))}},
    {Option.Some(false), new[] {("A", Option.Some(false))}},
    {Option.None<bool>(), new[] {("A", Option.None<bool>())}}
  };

  public static TheoryData<Option<bool>, CMap<string, Option<bool>>> ConcreteArrangementParameters => new()
  {
    {Option.Some(true), new CMap<string, Option<bool>>()},
    {Option.Some(false), new CMap<string, Option<bool>>()},
    {Option.None<bool>(), new CMap<string, Option<bool>>()},
    {Option.Some(true), new CMap<string, Option<bool>>().Set("A", Option.Some(true))},
    {Option.Some(false), new CMap<string, Option<bool>>().Set("A", Option.Some(false))},
    {Option.None<bool>(), new CMap<string, Option<bool>>().Set("A", Option.None<bool>())}
  };

  [Theory]
  [MemberData(nameof(ConcreteArrangementParameters))]
  public static void ConstructValidConcreteArrangements(Option<bool> invariant, CMap<string, Option<bool>> neighbors)
  {
    var a = new CMapArrangement<string>(invariant, neighbors);
    Assert.Equal(invariant, a.Invariant);
    Assert.Equal(neighbors, a.Neighbors);
  }

  [Theory]
  [MemberData(nameof(ConcreteArrangementEnumerable))]
  public static void ConstructValidConcreteArrangementsEnumerable(Option<bool> invariant,
    IEnumerable<(string, Option<bool>)> neighbors)
  {
    var n = neighbors.ToArray();
    var a = new CMapArrangement<string>(invariant, n);
    Assert.Equal(invariant, a.Invariant);
    foreach (var (m, v) in n) Assert.Equal(v, a[m]);
  }
}

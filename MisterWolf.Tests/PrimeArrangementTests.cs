using Xunit;

namespace MisterWolf.Tests;

public static class PrimeArrangementTests
{
  private static readonly List<bool?[]> IrreducibleArrangements = new()
  {
    new bool?[] {true, null, true},
    new bool?[] {null, false, true},
    new bool?[] {false, false, null},
  };

  private static readonly List<bool?[]> ReducibleArrangements = new()
  {
    new bool?[] {true, false, true},
    new bool?[] {true, true, true},
    new bool?[] {false, false, false},
    new bool?[] {false, false, true}
  };

  private static readonly List<bool?[]> DoublyReducibleArrangements = new()
  {
    new bool?[] {true, false, true},
    new bool?[] {true, true, true},
    new bool?[] {true, false, false},
    new bool?[] {true, true, false},
  };

  private static void TestPrimeArrangementAlreadySimplest(List<bool?[]> arrangements)
  {
    Assert.Equal(arrangements, PrimeArrangements.SimplifyArrangements(arrangements, true));
  }

  [Fact]
  public static void IrreducibleArrangementsDoNotSimplify()
  {
    TestPrimeArrangementAlreadySimplest(IrreducibleArrangements);
  }

  [Fact]
  public static void SingleReduceUntilIrreducible()
  {
    Assert.Equivalent(IrreducibleArrangements, PrimeArrangements.SimplifyArrangements(ReducibleArrangements, true),
      true);
  }

  [Fact]
  public static void DoubleReduceUntilIrreducible()
  {
    Assert.Equivalent(new List<bool?[]> {new bool?[] {true, null, null}},
      PrimeArrangements.SimplifyArrangements(DoublyReducibleArrangements, true), true);
  }
}

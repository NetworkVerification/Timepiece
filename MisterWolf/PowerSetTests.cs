using Xunit;

namespace MisterWolf;

public static class PowerSetTests
{
  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public static void TestBitPSetSize(int elements)
  {
    Assert.Equal(1 << elements, PowerSet.BitPSet(elements).Count());
  }
}

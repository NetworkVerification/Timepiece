using System.Collections;
using Xunit;

namespace MisterWolf;

/// <summary>
///   Power set utility functions.
/// </summary>
public static class PowerSet
{
  /// <summary>
  ///   Return an array of bit arrays representing all possible
  ///   choices from the given number of elements.
  /// </summary>
  /// <param name="elements">The number of elements of each bit array.</param>
  /// <returns>
  ///   An enumerable of bitarrays representing the power set of the elements,
  ///   ranging from 0 to 2^(number of neighbors) - 1.
  /// </returns>
  public static IEnumerable<BitArray> BitPSet(int elements)
  {
    // the number of arrangements is 2^(elements), i.e. 1 bit-shifted elements times
    var numberOfArrangements = 1 << elements;
    // initialize a new list of bit arrays where each bit represents an element being on or off
    return Enumerable.Range(0, numberOfArrangements).Select(i => new BitArray(BitConverter.GetBytes(i)));
  }

  /// <summary>
  ///   Return the power set of an enumerable of elements.
  /// </summary>
  /// <param name="set"></param>
  /// <returns></returns>
  private static IEnumerable<IEnumerable<TElement>> PSet<TElement>(IEnumerable<TElement> set)
  {
    var sets = new List<IEnumerable<TElement>> {Enumerable.Empty<TElement>()};
    return set.Aggregate(sets,
      // given the current set of sets, add new sets which extend them all by an element
      (currentSets, element) =>
        currentSets.Concat(currentSets.Select(s => s.Concat(new[] {element}))).ToList());
  }
}

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

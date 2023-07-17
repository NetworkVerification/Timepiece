using System.Collections.Immutable;

namespace MisterWolf;

/// <summary>
///   Power set utility functions.
/// </summary>
public static class PowerSet
{
  /// <summary>
  ///   Return an enumerable of boolean lists representing all possible
  ///   choices from the given number of elements.
  /// </summary>
  /// <param name="elements">The number of elements of each boolean list.</param>
  /// <returns>
  ///   An enumerable of immutable lists representing the power set of the elements,
  ///   ranging from 0 to 2^(number of elements) - 1.
  /// </returns>
  public static IEnumerable<IReadOnlyList<bool>> BitPSet(int elements)
  {
    // the number of lists is 2^(elements), i.e. 1 bit-shifted elements times
    IEnumerable<ImmutableList<bool>> sets = new List<ImmutableList<bool>>(1 << elements)
      {Enumerable.Repeat(false, elements).ToImmutableList()};
    for (var i = 0; i < elements; i++)
    {
      var i1 = i;
      var enumerable = sets.ToArray();
      sets = enumerable.Concat(enumerable.Select(b => b.SetItem(i1, true)));
    }

    return sets;
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

using System.Collections;

namespace MisterWolf;

class ArrangementEqualityComparer : EqualityComparer<bool?[]>
{
  public override bool Equals(bool?[]? x, bool?[]? y)
  {
    if (x is null && y is null)
      return true;
    if (x is null || y is null)
      return false;
    if (x.Length != y.Length)
      return false;

    var allEqual = true;
    for (var i = 0; i < x.Length; i++)
    {
      allEqual &= x[i] == y[i];
    }

    return allEqual;
  }

  public override int GetHashCode(bool?[] obj)
  {
    throw new NotImplementedException();
  }
}

public static class PrimeArrangements
{
  /// <summary>
  /// Return a simplified version of the given dictionary of node neighbor arrangements.
  /// Applies a basic implementation of the Quine-McCluskey algorithm to identify and combine minterms.
  /// Bitarrays are replaced by List &lt;bool?&gt; terms, where each term can be null to indicate "don't care".
  /// </summary>
  /// <param name="arrangements"></param>
  /// <param name="recurse"></param>
  /// <returns></returns>
  public static List<bool?[]> SimplifyArrangements(
    List<bool?[]> arrangements, bool recurse)
  {
    if (!recurse)
    {
      return arrangements;
    }

    var simplified = new List<bool?[]>();
    // combine all arrangements that differ by one bit
    {
      for (var i = 0; i < arrangements.Count; i++)
      {
        for (var j = i; j < arrangements.Count; j++)
        {
          // take the XOR to find out how many bits change between the two bitarrays
          // we want to combine bitarrays when the result has at most one bit set
          // and where all "don't care" bits are aligned
          var atMostOneDifferingBit = false;
          var dontCareBitsAligned = true;
          int? differingBit = null;
          for (var k = 0; k < arrangements[i].Length; k++)
          {
            // make sure that all don't care bits match up
            if ((arrangements[i][k] is null && arrangements[j][k] is not null) ||
                (arrangements[i][k] is not null && arrangements[j][k] is null))
            {
              dontCareBitsAligned = false;
              break;
            }

            if (arrangements[i][k] != arrangements[j][k])
            {
              if (atMostOneDifferingBit)
              {
                // if we already have a differing index, then set to false and exit the loop
                atMostOneDifferingBit = false;
                break;
              }

              differingBit = k;
              atMostOneDifferingBit = true;
            }
          }

          if (dontCareBitsAligned && atMostOneDifferingBit && differingBit is not null)
          {
            // combine at the differing Bit
            var combined = new bool?[arrangements[i].Length];
            for (var k = 0; k < arrangements[i].Length; k++)
            {
              if (k == differingBit)
              {
                combined[k] = null;
              }
              else
              {
                combined[k] = arrangements[i][k];
              }
            }

            // if none of the already-found minterms match this minterm, add it
            if (simplified.All(t => !t.SequenceEqual(combined)))
              simplified.Add(combined);
          }
        }
      }
    }

    // recurse; if no further simplified terms are returned, then stop
    return SimplifyArrangements(simplified.Count == 0 ? arrangements : simplified, simplified.Count != 0);
  }

  public static IEnumerable<bool?[]> SimplifyArrangements(IEnumerable<BitArray> arrangements)
  {
    return SimplifyArrangements(
      arrangements.Select(ba =>
      {
        var a = new bool?[ba.Count];
        for (var i = 0; i < ba.Count; i++)
        {
          a[i] = ba[i];
        }

        return a;
      }).ToList(), true);
  }
}

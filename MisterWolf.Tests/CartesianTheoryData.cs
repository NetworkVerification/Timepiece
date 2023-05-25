using System.Diagnostics.Contracts;
using Xunit;

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

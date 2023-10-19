using System.Diagnostics.Contracts;
using Xunit;

namespace MisterWolf.Tests;

/// <summary>
///   A TheoryData subclass for generating tests that take the Cartesian product of two enumerables.
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

public class CartesianTheoryData<T1, T2, T3> : TheoryData<T1, T2, T3>
{
  public CartesianTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3)
  {
    Contract.Assert(data1 != null && data1.Any());
    Contract.Assert(data2 != null && data2.Any());
    Contract.Assert(data3 != null && data3.Any());

    var array2 = data2 as T2[] ?? data2.ToArray();
    var array3 = data3 as T3[] ?? data3.ToArray();
    foreach (var t1 in data1)
    foreach (var t2 in array2)
    foreach (var t3 in array3)
      Add(t1, t2, t3);
  }
}

using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests.Networks;

using LpRoute = Pair<BigInteger, BigInteger>;

public class LocalPref<TV, TS> : Network<Pair<BigInteger, BigInteger>, TV, TS> where TV : notnull
{
  public LocalPref(Digraph<TV> digraph,
    Dictionary<TV, Zen<LpRoute>> initialValues,
    SymbolicValue<TS>[] symbolics)
    : base(digraph,
      digraph.MapEdges(_ => Lang.Product(Lang.Identity<BigInteger>(), Lang.Incr(new BigInteger(1)))),
      Merge,
      initialValues,
      symbolics)
  {
  }

  /// <summary>
  ///   The merge function for the simple path length network.
  /// </summary>
  private static Zen<LpRoute> Merge(Zen<LpRoute> r1,
    Zen<LpRoute> r2)
  {
    var (r1First, r1Second) = (r1.Item1(), r1.Item2());
    var (r2First, r2Second) = (r2.Item1(), r2.Item2());
    var cmp = If(r1Second < r2Second, r1, r2);
    return If(r1First < r2First, r1, If(r1First == r2First, cmp, r2));
  }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

using LpRoute = Pair<BigInteger, BigInteger>;

public class LocalPref<TS> : AnnotatedNetwork<LpRoute, TS>
{
  public LocalPref(Topology topology,
    Dictionary<string, Zen<LpRoute>> initialValues,
    Dictionary<string, Func<Zen<LpRoute>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime,
    SymbolicValue<TS>[] symbolics)
    : base(topology,
      topology.MapEdges(_ => Lang.Product(Lang.Identity<BigInteger>(), Lang.Incr(new BigInteger(1)))),
      Merge,
      initialValues,
      annotations,
      topology.MapNodes(_ => Lang.Finally<LpRoute>(convergeTime, ReachabilityProperty)),
      topology.MapNodes<Func<Zen<LpRoute>, Zen<bool>>>(_ => ReachabilityProperty),
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

  /// <summary>
  ///   Final assertion we want to check for the stable paths encoding that removes time.
  /// </summary>
  private static Zen<bool> ReachabilityProperty(Zen<LpRoute> r)
  {
    return r.Item2() < new BigInteger(10);
  }
}

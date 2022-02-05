using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public class ShortestPath<TS> : Network<Option<BigInteger>, TS>
{
  public ShortestPath(Topology topology,
    Dictionary<string, Zen<Option<BigInteger>>> initialValues,
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
    SymbolicValue<TS>[] symbolics,
    BigInteger convergeTime
  ) : base(topology, topology.ForAllEdges(_ => Lang.Omap(Lang.Incr(BigInteger.One))), Lang.Omap2<BigInteger>(Min),
    initialValues,
    annotations,
    topology.ForAllNodes(_ => Lang.Finally(convergeTime, Lang.IsSome<BigInteger>())),
    topology.ForAllNodes(_ => Lang.IsSome<BigInteger>()),
    symbolics)
  {
  }
}
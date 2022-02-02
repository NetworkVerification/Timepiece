using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo;

public class ShortestPath<TS> : Network<Option<BigInteger>, TS>
{
  public ShortestPath(Topology topology,
    Dictionary<string, Zen<Option<BigInteger>>> initialValues,
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<Zen<TS>, Func<Zen<TS>, Zen<bool>>> symbolics,
    BigInteger convergeTime
  ) : base(topology, topology.ForAllEdges(_ => Lang.Omap(Lang.Incr(1))), Lang.Omap2<BigInteger>(Min), initialValues,
    annotations,
    new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>(),
    new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>(),
    symbolics)
  {
    this.annotations = annotations;
    foreach (var node in topology.nodes)
    {
      modularProperties.Add(node, Lang.Finally(convergeTime, Lang.IsSome<BigInteger>()));
      monolithicProperties.Add(node, Lang.IsSome<BigInteger>());
    }
  }
}
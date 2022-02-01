using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo;

public class ShortestPath : Network<Option<uint>>
{
  public ShortestPath(Topology topology,
    Dictionary<string, Zen<Option<uint>>> initialValues,
    Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime
  ) : base(topology, topology.ForAllEdges(_ => Lang.Omap(Lang.Incr(1))), Lang.Omap2<uint>(Min), initialValues,
    annotations,
    new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>(),
    new Dictionary<string, Func<Zen<Option<uint>>, Zen<bool>>>(),
    new Dictionary<Zen<object>, Zen<bool>>()
  )
  {
    this.annotations = annotations;
    foreach (var node in topology.nodes)
    {
      modularProperties.Add(node, Lang.Finally(convergeTime, Lang.IsSome<uint>()));
      monolithicProperties.Add(node, Lang.IsSome<uint>());
    }
  }
}
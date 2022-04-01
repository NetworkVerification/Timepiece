using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Datatypes;
using ZenLib;
using static ZenLib.Zen;

namespace Karesansui.Networks;

public class BgpNetwork : Network<Option<Bgp>, Bgp>
{
  public BgpNetwork(Topology topology,
    Dictionary<string, Zen<Option<Bgp>>> initialValues,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime,
    SymbolicValue<Bgp>[] symbolics) : base(topology,
    topology.ForAllEdges(e => Lang.Bind(Transfer(e))), Lang.Omap2<Bgp>(BgpExtensions.Min),
    initialValues, annotations, topology.ForAllNodes(_ => Lang.Finally<Option<Bgp>>(convergeTime, Option.IsSome)),
    topology.ForAllNodes(_ => Lang.IsSome<Bgp>()), symbolics)
  {
  }

  private static Func<Zen<Bgp>, Zen<Option<Bgp>>> Transfer((string, string) edge)
  {
    var (src, snk) = edge;
    return x => If(x.HasTag(snk), Option.None<Bgp>(),
      Option.Create(x.SetAsLength(x.GetAsLength() + new BigInteger(1)).AddTag(src)));
  }
}

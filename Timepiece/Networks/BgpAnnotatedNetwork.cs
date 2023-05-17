using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Datatypes;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class BgpAnnotatedNetwork : AnnotatedNetwork<Option<Bgp>, Bgp>
{
  public BgpAnnotatedNetwork(Topology topology,
    Dictionary<string, Zen<Option<Bgp>>> initialValues,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime,
    SymbolicValue<Bgp>[] symbolics) : base(topology,
    topology.MapEdges(e => Lang.Bind(Transfer(e))), Lang.Omap2<Bgp>(Bgp.Min),
    initialValues, annotations, topology.MapNodes(_ => Lang.Finally<Option<Bgp>>(convergeTime, Option.IsSome)),
    topology.MapNodes(_ => Lang.IsSome<Bgp>()), symbolics)
  {
  }

  private static Func<Zen<Bgp>, Zen<Option<Bgp>>> Transfer((string, string) edge)
  {
    var (src, snk) = edge;
    return x => If(x.HasTag(snk), Option.None<Bgp>(),
      Option.Create(x.IncrementAsLength().AddTag(src)));
  }
}

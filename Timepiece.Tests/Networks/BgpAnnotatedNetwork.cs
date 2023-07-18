using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Datatypes;
using Timepiece.Networks;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests.Networks;

public class BgpAnnotatedNetwork<TV, TS> : AnnotatedNetwork<Option<Bgp>, TV, TS> where TV : notnull
{
  public BgpAnnotatedNetwork(Digraph<TV> digraph,
    Dictionary<TV, Zen<Option<Bgp>>> initialValues,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime,
    SymbolicValue<TS>[] symbolics) : base(digraph,
    digraph.MapEdges(e => Lang.Bind(Transfer(e))), Lang.Omap2<Bgp>(Bgp.Min),
    initialValues, annotations, digraph.MapNodes(_ => Lang.Finally<Option<Bgp>>(convergeTime, Option.IsSome)),
    digraph.MapNodes(_ => Lang.IsSome<Bgp>()), symbolics)
  {
  }

  private static Func<Zen<Bgp>, Zen<Option<Bgp>>> Transfer((TV, TV) edge)
  {
    var (src, snk) = edge;
    return x => If(x.HasTag($"{snk}"), Option.None<Bgp>(),
      Option.Create(x.IncrementAsLength().AddTag($"{src}")));
  }
}

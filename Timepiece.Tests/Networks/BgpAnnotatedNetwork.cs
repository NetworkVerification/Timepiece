using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.DataTypes;
using Timepiece.Networks;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests.Networks;

public class BgpAnnotatedNetwork<TV, TS> : AnnotatedNetwork<Option<Bgp>, TV, TS> where TV : notnull
{
  public BgpAnnotatedNetwork(Digraph<TV> digraph,
    Dictionary<TV, Zen<Option<Bgp>>> initialValues,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<bool>>> monolithicProperties,
    SymbolicValue<TS>[] symbolics) : base(digraph,
    digraph.MapEdges(e => Lang.Bind(Transfer(e))), Lang.Omap2<Bgp>(Bgp.Min),
    initialValues, annotations, modularProperties,
    monolithicProperties, symbolics)
  {
  }

  /// <summary>
  /// Return a per-edge transfer function where, for a given edge source-target,
  /// if the route contains a tag matching the target node,
  /// then the route is dropped (mimicking BGP loop detection)
  /// and otherwise the function adds a tag matching the source node.
  /// </summary>
  /// <param name="edge"></param>
  /// <returns></returns>
  private static Func<Zen<Bgp>, Zen<Option<Bgp>>> Transfer((TV, TV) edge)
  {
    var (src, snk) = edge;
    return x => If(x.HasTag($"{snk}"), Option.None<Bgp>(),
      Option.Create(x.IncrementAsLength().AddTag($"{src}")));
  }
}

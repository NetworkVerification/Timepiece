using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.Networks;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests.Networks;

public class FaultTolerance<T, TV> : AnnotatedNetwork<Option<T>, TV, (TV, TV)> where TV : notnull
{
  public FaultTolerance(Topology<TV> topology,
    Dictionary<(TV, TV), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<TV, Zen<Option<T>>> initialValues,
    Func<SymbolicValue<(TV, TV)>[], Dictionary<TV, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>>
      annotations,
    Dictionary<TV, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<TV, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
    Zen<FSeq<(TV, TV)>> failedEdges, uint numFailed) : base(topology,
    new Dictionary<(TV, TV), Func<Zen<Option<T>>, Zen<Option<T>>>>(),
    Lang.Omap2(mergeFunction), initialValues,
    new Dictionary<TV, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>(),
    modularProperties, monolithicProperties,
    CreateSymbolics(topology, failedEdges, numFailed))
  {
    Annotations = annotations(Symbolics);
    TransferFunction = Transfer(transferFunction, Symbolics);
  }

  public FaultTolerance(Network<T, TV, Unit> net,
    Dictionary<TV, Zen<Option<T>>> initialValues,
    Func<SymbolicValue<(TV, TV)>[], Dictionary<TV, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>>
      annotations,
    Dictionary<TV, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<TV, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
    Zen<FSeq<(TV, TV)>> failedEdges, uint numFailed) : base(net.Topology,
    new Dictionary<(TV, TV), Func<Zen<Option<T>>, Zen<Option<T>>>>(), Lang.Omap2(net.MergeFunction),
    initialValues,
    new Dictionary<TV, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>(), modularProperties, monolithicProperties,
    CreateSymbolics(net.Topology, failedEdges, numFailed))
  {
    Annotations = annotations(Symbolics);
    TransferFunction = Transfer(net.TransferFunction, Symbolics);
  }

  /// <summary>
  ///   Return true if the given edge is in the sequence of failed edges, and false otherwise.
  /// </summary>
  /// <param name="failedEdges"></param>
  /// <param name="edge"></param>
  /// <returns></returns>
  public static Zen<bool> IsFailed(IEnumerable<SymbolicValue<(TV, TV)>> failedEdges, (TV, TV) edge)
  {
    return failedEdges.Aggregate(False(), (current, e) => Or(current, e.EqualsValue(edge)));
  }

  private static SymbolicValue<(TV, TV)>[] CreateSymbolics(
    Topology<TV> topology,
    Zen<FSeq<(TV, TV)>> failedEdges,
    uint numFailed)
  {
    var symbolics = new SymbolicValue<(TV, TV)>[numFailed];
    for (var i = 0; i < numFailed; i++)
    {
      var e = new SymbolicValue<(TV, TV)>($"e{i}")
      {
        Constraint = p =>
          And(topology.FoldEdges(False(), (b, edge) => Or(b, Constant(edge) == p)), failedEdges.Contains(p))
      };
      symbolics[i] = e;
    }

    return symbolics;
  }

  private static Dictionary<(TV, TV), Func<Zen<Option<T>>, Zen<Option<T>>>> Transfer(
    Dictionary<(TV, TV), Func<Zen<T>, Zen<T>>> inner, SymbolicValue<(TV, TV)>[] failedEdges)
  {
    var lifted = new Dictionary<(TV, TV), Func<Zen<Option<T>>, Zen<Option<T>>>>();
    foreach (var (edge, f) in inner)
      lifted[edge] =
        Lang.Test(_ => IsFailed(failedEdges, edge), Lang.Const(Option.None<T>()), Lang.Omap(f));

    return lifted;
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class FaultTolerance<T> : Network<Option<T>, (string, string)>
{
  public FaultTolerance(Topology topology,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<string, Zen<Option<T>>> initialValues,
    Func<SymbolicValue<(string, string)>[], Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>>
      annotations,
    Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
    Zen<FSeq<(string, string)>> failedEdges, uint numFailed) : base(topology,
    new Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>>(),
    Lang.Omap2(mergeFunction), initialValues,
    new Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>(),
    modularProperties, monolithicProperties,
    CreateSymbolics(topology, failedEdges, numFailed))
  {
    Annotations = annotations(Symbolics);
    TransferFunction = Transfer(transferFunction, Symbolics);
  }

  public FaultTolerance(Network<T, object> net,
    Dictionary<string, Zen<Option<T>>> initialValues,
    Func<SymbolicValue<(string, string)>[], Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>>
      annotations,
    Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
    Zen<FSeq<(string, string)>> failedEdges, uint numFailed) : base(net.Topology,
    new Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>>(), Lang.Omap2(net.MergeFunction),
    initialValues,
    new Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>>(), modularProperties, monolithicProperties,
    CreateSymbolics(net.Topology, failedEdges, numFailed))
  {
    Annotations = annotations(Symbolics);
    TransferFunction = Transfer(net.TransferFunction, Symbolics);
  }

  /// <summary>
  /// Return true if the given edge is in the sequence of failed edges, and false otherwise.
  /// </summary>
  /// <param name="failedEdges"></param>
  /// <param name="edge"></param>
  /// <returns></returns>
  public static Zen<bool> IsFailed(IEnumerable<SymbolicValue<(string, string)>> failedEdges, (string, string) edge)
  {
    return failedEdges.Aggregate(False(), (current, e) => Or(current, e.EqualsValue(edge)));
  }

  private static SymbolicValue<(string, string)>[] CreateSymbolics(
    Topology topology,
    Zen<FSeq<(string, string)>> failedEdges,
    uint numFailed)
  {
    var symbolics = new SymbolicValue<(string, string)>[numFailed];
    for (var i = 0; i < numFailed; i++)
    {
      var e = new SymbolicValue<(string, string)>($"e{i}")
      {
        Constraint = p =>
          And(topology.FoldEdges(False(), (b, edge) => Or(b, Constant(edge) == p)), failedEdges.Contains(p))
      };
      symbolics[i] = e;
    }

    return symbolics;
  }

  private static Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>> Transfer(
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> inner, SymbolicValue<(string, string)>[] failedEdges)
  {
    var lifted = new Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>>();
    foreach (var (edge, f) in inner)
      lifted[edge] =
        Lang.Test(_ => IsFailed(failedEdges, edge), Lang.Const(Option.None<T>()), Lang.Omap(f));

    return lifted;
  }
}

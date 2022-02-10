using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public class FaultTolerance<T> : Network<Option<T>, (string, string)>
{
  /// <summary>
  /// Edges which have failed in the given topology.
  /// </summary>
  private Zen<IList<(string, string)>> failedEdges;

  public FaultTolerance(Topology topology,
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> transferFunction,
    Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
    Dictionary<string, Zen<Option<T>>> initialValues,
    Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
    Zen<IList<(string, string)>> failedEdges) : base(topology,
    Transfer(transferFunction, failedEdges),
    Lang.Omap2(mergeFunction), initialValues, annotations, modularProperties, monolithicProperties,
    Symbolics(topology, failedEdges))
  {
    this.failedEdges = failedEdges;
  }

  public FaultTolerance(Network<T, object> net,
    Dictionary<string, Zen<Option<T>>> initialValues,
    Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
    Zen<IList<(string, string)>> failedEdges) : base(net.Topology,
    Transfer(net.TransferFunction, failedEdges), Lang.Omap2(net.MergeFunction), initialValues,
    annotations, modularProperties, monolithicProperties, Symbolics(net.Topology, failedEdges))
  {
  }

  private static SymbolicValue<(string, string)>[] Symbolics(
    Topology topology,
    Zen<IList<(string, string)>> failedEdges)
  {
    var e = new SymbolicValue<(string, string)>("e")
    {
      Constraint = p =>
        And(topology.FoldEdges(False(), (b, edge) => Or(b, Constant(edge) == p)), failedEdges.Contains(p))
    };
    return new[] {e};
  }

  private static Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>> Transfer(
    Dictionary<(string, string), Func<Zen<T>, Zen<T>>> inner, Zen<IList<(string, string)>> failedEdges)
  {
    var lifted = new Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>>();
    foreach (var (edge, f) in inner)
      lifted[edge] =
        Lang.Test(_ => failedEdges.Contains(edge), Lang.Const(Option.None<T>()), Lang.Omap(f));

    return lifted;
  }
}

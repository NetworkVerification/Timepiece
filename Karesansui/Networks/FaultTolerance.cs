using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public class FaultTolerance<T> : Network<Option<T>, (string, string)>
{
    /// <summary>
    ///     Edges which have failed in the given topology.
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
        Symbolics(failedEdges))
    {
        this.failedEdges = failedEdges;
    }

    private static Dictionary<Zen<(string, string)>, Func<Zen<(string, string)>, Zen<bool>>> Symbolics(
        Zen<IList<(string, string)>> failedEdges)
    {
        var e = Symbolic<(string, string)>();
        return new Dictionary<Zen<(string, string)>, Func<Zen<(string, string)>, Zen<bool>>>
        {
            {e, failedEdges.Contains}
        };
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

    public static FaultTolerance<Unit> Sound()
    {
        Console.WriteLine("Sound annotations:");
        var topology = Default.Complete(3);

        var initialValues = new Dictionary<string, Zen<Option<Unit>>>
        {
            {"A", Option.Some(new Unit())},
            {"B", Option.None<Unit>()},
            {"C", Option.None<Unit>()}
        };

        var annotations = new Dictionary<string, Func<Zen<Option<Unit>>, Zen<BigInteger>, Zen<bool>>>
        {
            {"A", Lang.Globally(Lang.IsSome<Unit>())},
            {"B", Lang.Finally(new BigInteger(1), Lang.IsSome<Unit>())},
            {"C", Lang.Finally(new BigInteger(1), Lang.IsSome<Unit>())}
        };

        var modularProperties = topology.ForAllNodes(_ => Lang.Finally(new BigInteger(1), Lang.IsSome<Unit>()));
        var monolithicProperties = topology.ForAllNodes(_ => Lang.IsSome<Unit>());

        var failedEdges = Symbolic<IList<(string, string)>>(topology.nEdges);

        return new FaultTolerance<Unit>(topology, topology.ForAllEdges(_ => Lang.Identity<Unit>()),
            (_, _) => new Unit(), initialValues,
            annotations, modularProperties, monolithicProperties, failedEdges);
    }
}
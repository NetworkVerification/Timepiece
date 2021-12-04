using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo;

using DistMap = Dict<string, Option<uint>>;

public static class AllPairs
{
    public static Network<DistMap> Net(
        Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>> annotations)
    {
        var topology = Default.Path(4);

        var initialValues =
            topology.ForAllNodes(node =>
                Constant(topology.ToNodeDict(n => node.Equals(n) ? Option.Some(0U) : Option.None<uint>())));

        var convergeTime = new BigInteger(10);
        var modularProperties =
            topology.ForAllNodes(_ => Lang.After<DistMap>(convergeTime, ReachabilityAssertionStable));

        var monolithicProperties = topology.ForAllNodes(_ => ReachabilityAssertionStable);

        var assumptions = Array.Empty<Zen<bool>>();

        return new Network<DistMap>(topology, topology.ForAllEdges(_ => TransferFunction), MergeFunction, initialValues,
            annotations,
            modularProperties, monolithicProperties, assumptions);
    }

    public static Network<DistMap> Sound()
    {
        var annotations = new Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>>
        {
            {"A", Lang.After<DistMap>(new BigInteger(10), ReachabilityAssertionStable)},
            {"B", Lang.After<DistMap>(new BigInteger(10), ReachabilityAssertionStable)},
            {"C", Lang.After<DistMap>(new BigInteger(10), ReachabilityAssertionStable)},
            {"D", Lang.After<DistMap>(new BigInteger(10), ReachabilityAssertionStable)}
        };
        return Net(annotations);
    }

    private static Zen<bool> ReachabilityAssertionStable(Zen<DistMap> route)
    {
        return route.All(pair => Lang.IsSome<uint>()(pair.Item2()));
    }

    private static Zen<DistMap> MergeFunction(Zen<DistMap> r1, Zen<DistMap> r2)
    {
        return r1.Zip(r2, Lang.Omap2<uint>(Min));
    }

    private static Zen<DistMap> TransferFunction(Zen<DistMap> route)
    {
        return route.ForEach(Lang.Omap(Lang.Incr(1)));
    }
}
using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo;

/// <summary>
///     A network with a boolean routing algebra.
/// </summary>
public class BoolNet : Network<bool>
{
    public BoolNet(Topology topology,
        Dictionary<string, Zen<bool>> initialValues,
        Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
        BigInteger convergeTime)
        : base(topology, topology.ForAllEdges(_ => Lang.Identity<bool>()),
            Or, initialValues, annotations,
            topology.ForAllNodes(_ => Lang.After(convergeTime, Lang.Identity<bool>())),
            topology.ForAllNodes(_ => Lang.Identity<bool>()),
        Array.Empty<Zen<bool>>())
    {
    }
}
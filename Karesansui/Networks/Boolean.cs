using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

/// <summary>
///     A network with a boolean routing algebra.
/// </summary>
public class Boolean : Network<bool, Unit>
{
    public Boolean(Topology topology,
        Dictionary<string, Zen<bool>> initialValues,
        Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
        BigInteger convergeTime)
        : base(topology, topology.ForAllEdges(_ => Lang.Identity<bool>()),
            Or, initialValues, annotations,
            topology.ForAllNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
            topology.ForAllNodes(_ => Lang.Identity<bool>()),
            new Dictionary<Zen<Unit>, Func<Zen<Unit>, Zen<bool>>>())
    {
    }

    public static Boolean Sound()
    {
        Console.WriteLine("Sound annotations:");
        var topology = Default.Path(2);

        var initialValues = topology.ForAllNodes(n => Eq<string>(n, "A"));
        var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
        {
            {"A", Lang.Globally(Lang.Identity<bool>())},
            {"B", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())}
        };
        return new Boolean(topology, initialValues, annotations, new BigInteger(5));
    }
}
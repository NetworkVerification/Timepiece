using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    /// <summary>
    /// A network with a boolean routing algebra.
    /// </summary>
    public class BoolNet : Network<bool>
    {
        public BoolNet(Topology topology,
            Dictionary<string, bool> initialValues,
            Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
            BigInteger convergeTime)
            : base(topology, topology.ForAllEdges(_ => Identity),
            Merge, initialValues, annotations, topology.ForAllNodes(_ => Lang.After<bool>(convergeTime, Identity)),
            topology.ForAllNodes(_ => Identity))
        {
        }

        public static Zen<bool> Identity(Zen<bool> route)
        {
            return route;
        }

        public static Zen<bool> Merge(Zen<bool> r1, Zen<bool> r2)
        {
            return Or(r1, r2);
        }
    }
}
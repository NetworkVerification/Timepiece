using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    public class ShortestPath : Network<Option<uint>>
    {
        public ShortestPath(Topology topology,
            Dictionary<string, Option<uint>> initialValues,
            Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>> annotations,
            BigInteger convergeTime
        ) : base(topology, topology.ForAllEdges(_ => Transfer), Merge, initialValues, annotations,
            new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>(),
            new Dictionary<string, Func<Zen<Option<uint>>, Zen<bool>>>())
        {
            this.annotations = annotations;
            foreach (var node in topology.nodes)
            {
                modularProperties.Add(node, Lang.After<Option<uint>>(convergeTime, IsReachable));
                monolithicProperties.Add(node, IsReachable);
            }
        }

        /// <summary>
        /// The transfer function for the simple path length network.
        /// </summary>
        public static Zen<Option<uint>> Transfer(Zen<Option<uint>> route)
        {
            return route.Select(hops => hops + 1);
        }

        /// <summary>
        /// The merge function for the simple path length network.
        /// </summary>
        public static Zen<Option<uint>> Merge(Zen<Option<uint>> r1, Zen<Option<uint>> r2)
        {
            var min = Min(r1.Value(), r2.Value());
            return If(r1.HasValue(), If(r1.HasValue(), Some(min), r1), r2);
        }

        /// <summary>
        /// Final assertion we want to check for the stable paths encoding that removes time.
        /// </summary>
        public static Zen<bool> IsReachable(Zen<Option<uint>> r)
        {
            return r.HasValue();
        }
    }
}
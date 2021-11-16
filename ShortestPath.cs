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
        ) : base(topology, Transfer, Merge, initialValues, annotations,
            new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>(),
            new Dictionary<string, Func<Zen<Option<uint>>, Zen<bool>>>())
        {
            nodes = topology.nodes;
            neighbors = topology.neighbors;
            transferFunction = Transfer;
            mergeFunction = Merge;
            this.annotations = annotations;
            foreach (var node in nodes)
            {
                modularAssertions.Add(node, ReachabilityAssertionTime(convergeTime));
                monolithicAssertions.Add(node, ReachabilityAssertionStable);
            }
        }

        /// <summary>
        /// The transfer function for the simple path length network.
        /// </summary>
        public static Zen<Option<uint>> Transfer(Zen<Option<uint>> route)
        {
            return If(route.HasValue(), Some(route.Value() + 1), Option.None<uint>());
        }

        /// <summary>
        /// The merge function for the simple path length network.
        /// </summary>
        public static Zen<Option<uint>> Merge(Zen<Option<uint>> r1, Zen<Option<uint>> r2)
        {
            var r1HasNoRoute = Not(r1.HasValue());
            var r2HasNoRoute = Not(r2.HasValue());
            var min = Min(r1.Value(), r2.Value());
            return If(r1HasNoRoute, r2, If(r2HasNoRoute, r1, Some(min)));
        }

        /// <summary>
        /// Final assertion we want to check with respect to the network with time.
        /// </summary>
        public static Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>> ReachabilityAssertionTime(Zen<BigInteger> convergeTime)
        {
            return (r, time) => Implies(time > convergeTime, r.HasValue());
        }

        /// <summary>
        /// Final assertion we want to check for the stable paths encoding that removes time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionStable(Zen<Option<uint>> r)
        {
            return r != Option.None<uint>();
        }
    }
}
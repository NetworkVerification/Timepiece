using System;
using System.Numerics;
using System.Collections.Generic;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    public class Simple
    {
        /// <summary>
        /// Generate a simple example network.
        /// </summary>
        public static Network<Option<uint>> Net()
        {
            var nodes = new string[] { "A", "B", "C" };

            var neighbors = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "B" } },
                { "B", new List<string> { "A", "C" } },
                { "C", new List<string> { "B" } }
            };

            var initialValues = new Dictionary<string, Option<uint>>
            {
                { "A", Option.Some(0U) },
                { "B", Option.None<uint>() },
                { "C", Option.None<uint>() }
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var modularAssertions = new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionTime },
                { "B", ReachabilityAssertionTime },
                { "C", ReachabilityAssertionTime }
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var monolithicAssertions = new Dictionary<string, Func<Zen<Option<uint>>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionStable },
                { "B", ReachabilityAssertionStable },
                { "C", ReachabilityAssertionStable }
            };

            // sound annotations here. they are overapproximate but sufficient to prove what we want

            var annotations = new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", (route, time) => route == Option.Some(0U) },
                { "B", (route, time) => Implies(time > new BigInteger(0), route.HasValue()) },
                { "C", (route, time) => Implies(time > new BigInteger(1), route.HasValue()) }
            };

            return new Network<Option<uint>>(nodes, neighbors, Transfer, Merge, initialValues, annotations, modularAssertions, monolithicAssertions);

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
        public static Zen<bool> ReachabilityAssertionTime(Zen<Option<uint>> r, Zen<BigInteger> time)
        {
            return Implies(time > new BigInteger(10), r.HasValue());
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

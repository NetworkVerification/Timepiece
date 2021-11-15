using System;
using System.Numerics;
using System.Collections.Generic;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    public class LocalPref
    {
        /// <summary>
        /// Generate a simple example network.
        /// </summary>
        public static Network<Tuple<uint, uint>> SimpleNetwork()
        {
            var nodes = new string[] { "A", "B" };

            var neighbors = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "B" } },
                { "B", new List<string> { "A" } },
            };

            var initialValues = new Dictionary<string, Tuple<uint, uint>>
            {
                { "A", Tuple.Create(1U, 0U) },
                { "B", Tuple.Create(1U, 10U) },
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var modularAssertions = new Dictionary<string, Func<Zen<Tuple<uint, uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionTime },
                { "B", ReachabilityAssertionTime },
                { "C", ReachabilityAssertionTime }
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var monolithicAssertions = new Dictionary<string, Func<Zen<Tuple<uint, uint>>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionStable },
                { "B", ReachabilityAssertionStable },
                { "C", ReachabilityAssertionStable }
            };

            // sound annotations here. they are overapproximate but sufficient to prove what we want

            var annotations = new Dictionary<string, Func<Zen<Tuple<uint, uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", (route, time) => route == Tuple.Create(1U, 0U) },
                { "B", (route, time) => Implies(time > new BigInteger(0), route.HasValue()) },
                { "C", (route, time) => Implies(time > new BigInteger(1), route.HasValue()) }
            };

            return new Network<Tuple<uint, uint>>(nodes, neighbors, Transfer, Merge, initialValues, annotations, modularAssertions, monolithicAssertions);

        }

        /// <summary>
        /// The transfer function for the simple path length network.
        /// </summary>
        public static Zen<Tuple<uint, uint>> Transfer(Zen<Tuple<uint, uint>> route)
        {
            return Tuple.Create(route.Item1<uint>(), route.Item2<uint>() + 1);
        }

        /// <summary>
        /// The merge function for the simple path length network.
        /// </summary>
        public static Zen<Tuple<uint, uint>> Merge(Zen<Tuple<uint, uint>> r1, Zen<Tuple<uint, uint>> r2)
        {
            (Zen<uint> r1First, Zen<uint> r1Second) = r1;
            (Zen<uint> r2First, Zen<uint> r2Second) = r2;
            var min = Min(r1First, r2First);
            return If(r1HasNoRoute, r2, If(r2HasNoRoute, r1, Some(min)));
        }

        /// <summary>
        /// Final assertion we want to check with respect to the network with time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionTime(Zen<Tuple<uint, uint>> r, Zen<BigInteger> time)
        {
            return Implies(time > new BigInteger(10), r.HasValue());
        }

        /// <summary>
        /// Final assertion we want to check for the stable paths encoding that removes time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionStable(Zen<Tuple<uint, uint>> r)
        {
            return r != Option.None<uint>();
        }
    }
}

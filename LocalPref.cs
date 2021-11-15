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
        public static Network<Pair<uint, uint>> Net()
        {
            var nodes = new string[] { "A", "B" };

            var neighbors = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "B" } },
                { "B", new List<string> { "A" } },
            };

            var initialValues = new Dictionary<string, Pair<uint, uint>>
            {
                { "A", (1U, 0U) },
                { "B", (1U, 10U) },
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var modularAssertions = new Dictionary<string, Func<Zen<Pair<uint, uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionTime },
                { "B", ReachabilityAssertionTime },
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var monolithicAssertions = new Dictionary<string, Func<Zen<Pair<uint, uint>>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionStable },
                { "B", ReachabilityAssertionStable },
            };

            // sound annotations here. they are overapproximate but sufficient to prove what we want

            var annotations = new Dictionary<string, Func<Zen<Pair<uint, uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", (route, time) => route == Pair<uint, uint>(1U, 0U) },
                { "B", (route, time) => Or(route == Pair<uint, uint>(1U, 1U), route == Pair<uint, uint>(1U, 10U))},
            };

            return new Network<Pair<uint, uint>>(nodes, neighbors, Transfer, Merge, initialValues, annotations, modularAssertions, monolithicAssertions);

        }

        /// <summary>
        /// The transfer function for the simple path length network.
        /// </summary>
        public static Zen<Pair<uint, uint>> Transfer(Zen<Pair<uint, uint>> route)
        {
            return Pair(route.Item1(), route.Item2() + 1);
        }

        /// <summary>
        /// The merge function for the simple path length network.
        /// </summary>
        public static Zen<Pair<uint, uint>> Merge(Zen<Pair<uint, uint>> r1, Zen<Pair<uint, uint>> r2)
        {
            (Zen<uint> r1First, Zen<uint> r1Second) = (r1.Item1(), r1.Item2());
            (Zen<uint> r2First, Zen<uint> r2Second) = (r2.Item1(), r2.Item2());
            return If(Or(r1First < r2First, And(r1First == r2First, r2First < r2Second)), r1, r2);
        }

        /// <summary>
        /// Final assertion we want to check with respect to the network with time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionTime(Zen<Pair<uint, uint>> r, Zen<BigInteger> time)
        {
            return Implies(time > new BigInteger(10), r.Item2() < 10U);
        }

        /// <summary>
        /// Final assertion we want to check for the stable paths encoding that removes time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionStable(Zen<Pair<uint, uint>> r)
        {
            return r.Item2() < 10U;
        }
    }
}

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
        public static Network<Pair<uint, Option<uint>>> Net()
        {
            var nodes = new string[] { "A", "B" };

            var neighbors = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "B" } },
                { "B", new List<string> { "A" } },
            };

            var initialValues = new Dictionary<string, Pair<uint, Option<uint>>>
            {
                { "A", (1U, Option.Some<uint>(0U)) },
                { "B", (1U, Option.None<uint>()) },
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var modularAssertions = new Dictionary<string, Func<Zen<Pair<uint, Option<uint>>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionTime },
                { "B", ReachabilityAssertionTime },
            };

            // we want to prove that A and B are reachable beyond some point in time.

            var monolithicAssertions = new Dictionary<string, Func<Zen<Pair<uint, Option<uint>>>, Zen<bool>>>
            {
                { "A", ReachabilityAssertionStable },
                { "B", ReachabilityAssertionStable },
            };

            // sound annotations here. they are overapproximate but sufficient to prove what we want

            var annotations = new Dictionary<string, Func<Zen<Pair<uint, Option<uint>>>, Zen<BigInteger>, Zen<bool>>>
            {
                { "A", (route, time) => route == Pair<uint, Option<uint>>(1U, Option.Some(0U)) },
                { "B", (route, time) => And(route.Item1() == 1U, Implies(time > new BigInteger(0), route.Item2().HasValue()))},
            };

            return new Network<Pair<uint, Option<uint>>>(nodes, neighbors, Transfer, Merge, initialValues, annotations, modularAssertions, monolithicAssertions);

        }

        /// <summary>
        /// The transfer function for the simple path length network.
        /// </summary>
        public static Zen<Pair<uint, Option<uint>>> Transfer(Zen<Pair<uint, Option<uint>>> route)
        {
            var hops = route.Item2();
            return Pair(route.Item1(), If(hops.HasValue(), Some(hops.Value() + 1), Option.None<uint>()));
        }

        /// <summary>
        /// The merge function for the simple path length network.
        /// </summary>
        public static Zen<Pair<uint, Option<uint>>> Merge(Zen<Pair<uint, Option<uint>>> r1, Zen<Pair<uint, Option<uint>>> r2)
        {
            (Zen<uint> r1First, Zen<Option<uint>> r1Second) = (r1.Item1(), r1.Item2());
            (Zen<uint> r2First, Zen<Option<uint>> r2Second) = (r2.Item1(), r2.Item2());
            var r1HasNoRoute = Not(r1Second.HasValue());
            var r2HasNoRoute = Not(r2Second.HasValue());
            var min = Min(r1Second.Value(), r2Second.Value());
            var cmp = If(r1HasNoRoute, r2, If(r2HasNoRoute, r1, If(r1Second.Value() < r2Second.Value(), r1, r2)));
            return If(r1First < r2First, r1, If(r1First == r2First, cmp, r2));
        }

        /// <summary>
        /// Final assertion we want to check with respect to the network with time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionTime(Zen<Pair<uint, Option<uint>>> r, Zen<BigInteger> time)
        {
            return Implies(time > new BigInteger(10), r.Item2().HasValue());
        }

        /// <summary>
        /// Final assertion we want to check for the stable paths encoding that removes time.
        /// </summary>
        public static Zen<bool> ReachabilityAssertionStable(Zen<Pair<uint, Option<uint>>> r)
        {
            return r.Item2().HasValue();
        }
    }
}

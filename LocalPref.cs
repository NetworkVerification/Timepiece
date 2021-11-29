using System;
using System.Numerics;
using System.Collections.Generic;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    public static class LocalPref
    {
        /// <summary>
        /// Generate a simple example network.
        /// </summary>
        public static Network<Pair<uint, uint>> Net(
            Dictionary<string, Func<Zen<Pair<uint, uint>>, Zen<BigInteger>, Zen<bool>>> annotations)
        {
            var topology = Default.Path(2);

            var initialValues = new Dictionary<string, Pair<uint, uint>>
            {
                {"A", (1U, 0U)},
                {"B", (1U, 10U)},
            };

            var modularProperties =
                topology.ForAllNodes(_ => Lang.After<Pair<uint, uint>>(new BigInteger(10), ReachabilityProperty));

            var monolithicProperties = topology.ForAllNodes(_ => ReachabilityProperty);

            var transfer = topology.ForAllEdges(_ => Lang.Product(Lang.Identity<uint>(), Lang.Incr(1)));
            return new Network<Pair<uint, uint>>(topology, transfer, Merge, initialValues,
                annotations, modularProperties, monolithicProperties);
        }

        public static Network<Pair<uint, uint>> Sound()
        {
            Console.WriteLine($"Sound annotations:");
            var annotations = new Dictionary<string, Func<Zen<Pair<uint, uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                // NOTE: if we change the annotations from Item1() == 1 to Item1() <= 1,
                // the assertions will fail but the annotations still hold
                {"A", (route, _) => And(route.Item1() == 1, Implies(route.Item1() == 1, route.Item2() == 0))},
                {
                    "B",
                    (route, time) => And(route.Item1() == 1,
                        Implies(And(route.Item1() == 1, time > new BigInteger(0)), route.Item2() < 10))
                },
            };
            return Net(annotations);
        }

        public static Network<Pair<uint, uint>> Unsound()
        {
            Console.WriteLine($"Unsound annotations:");
            var annotations = new Dictionary<string, Func<Zen<Pair<uint, uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                {"A", (route, _) => And(route.Item1() <= 1, Implies(route.Item1() == 1, route.Item2() == 0))},
                {
                    "B",
                    (route, time) => And(route.Item1() <= 1,
                        Implies(And(route.Item1() == 1, time > new BigInteger(0)), route.Item2() < 10))
                },
            };
            return Net(annotations);
        }

        /// <summary>
        /// The merge function for the simple path length network.
        /// </summary>
        public static Zen<Pair<uint, uint>> Merge(Zen<Pair<uint, uint>> r1, Zen<Pair<uint, uint>> r2)
        {
            var (r1First, r1Second) = (r1.Item1(), r1.Item2());
            var (r2First, r2Second) = (r2.Item1(), r2.Item2());
            var cmp = If(r1Second < r2Second, r1, r2);
            return If(r1First < r2First, r1, If(r1First == r2First, cmp, r2));
        }

        /// <summary>
        /// Final assertion we want to check for the stable paths encoding that removes time.
        /// </summary>
        public static Zen<bool> ReachabilityProperty(Zen<Pair<uint, uint>> r)
        {
            return r.Item2() < 10;
        }
    }
}
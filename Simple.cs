using System;
using System.Numerics;
using System.Collections.Generic;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    public static class Simple
    {
        /// <summary>
        /// Generate a simple example network.
        /// </summary>
        public static Network<Option<uint>> Net(Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>> annotations)
        {
            // generates an "A"--"B"--"C" topology
            var topology = DefaultTopologies.Path(3);

            var initialValues = new Dictionary<string, Option<uint>>
            {
                {"A", Option.Some(0U)},
                {"B", Option.None<uint>()},
                {"C", Option.None<uint>()}
            };

            return new ShortestPath(topology, initialValues, annotations, new BigInteger(8));

        }

        public static Network<Option<uint>> Sound()
        {
            Console.WriteLine($"Sound annotations:");
            // sound annotations here. they are overapproximate but sufficient to prove what we want
            var annotations = new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                {"A", (route, _) => route == Option.Some(0U)},
                {"B", (route, time) => Implies(time > new BigInteger(0), route.HasValue())},
                {"C", (route, time) => Implies(time > new BigInteger(1), route.HasValue())}
            };
            return Net(annotations);
        }

        public static Network<Option<uint>> Unsound()
        {
            Console.WriteLine($"Unsound annotations:");
            // unsound annotations
            var annotations = new Dictionary<string, Func<Zen<Option<uint>>, Zen<BigInteger>, Zen<bool>>>
            {
                {"A", (route, _) => route == Option.Some(0U)},
                {"B", (route, _) => Not(route.HasValue())},
                {"C", (route, _) => Not(route.HasValue())}
            };
            return Net(annotations);
        }
    }
}

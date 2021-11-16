using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace ZenDemo
{
    using DistMap = Dictionary<string, Option<uint>>;

    public static class AllPairs
    {
        public static Network<DistMap> Net(
            Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>> annotations)
        {
            var topology = DefaultTopologies.Path(4);

            var initialValues = new Dictionary<string, DistMap>
            {
                {
                    "A", new DistMap
                        {{"A", Option.Some(0U)}}
                },
                {
                    "B", new DistMap
                        {{"B", Option.Some(0U)}}
                },
                {
                    "C", new DistMap
                        {{"C", Option.Some(0U)}}
                },
                {
                    "D", new DistMap
                        {{"D", Option.Some(0U)}}
                },
            };

            var modularAssertions = new Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>>
            {
                {"A", ReachabilityAssertionTime},
                {"B", ReachabilityAssertionTime},
                {"C", ReachabilityAssertionTime},
                {"D", ReachabilityAssertionTime},
            };

            var monolithicAssertions = new Dictionary<string, Func<Zen<DistMap>, Zen<bool>>>
            {
                {"A", ReachabilityAssertionStable},
                {"B", ReachabilityAssertionStable},
                {"C", ReachabilityAssertionStable},
                {"D", ReachabilityAssertionStable},
            };

            return new Network<DistMap>(topology, TransferFunction, MergeFunction, initialValues, annotations,
                modularAssertions, monolithicAssertions);
        }

        private static Zen<bool> ReachabilityAssertionStable(Zen<DistMap> route)
        {
            throw new NotImplementedException();
        }

        private static Zen<bool> ReachabilityAssertionTime(Zen<DistMap> route, Zen<BigInteger> time)
        {
            throw new NotImplementedException();
        }

        private static Zen<DistMap> MergeFunction(Zen<DistMap> r1, Zen<DistMap> r2)
        {
            throw new NotImplementedException();
        }

        private static Zen<DistMap> TransferFunction(Zen<DistMap> arg)
        {
            throw new NotImplementedException();
        }
    }
}
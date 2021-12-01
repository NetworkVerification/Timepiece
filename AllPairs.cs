using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace ZenDemo
{
    using DistMap = TotalMap<string, Option<uint>>;

    public static class AllPairs
    {
        public static Network<DistMap> Net(
            Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>> annotations)
        {
            var topology = Default.Path(4);

            var initialValues =
                topology.ForAllNodes(node =>
                    topology.ToNodeMap(_ => Option.None<uint>()).Update(node, Option.Some(0U)));
            
            var convergeTime = new BigInteger(10);
            var modularProperties =
                topology.ForAllNodes(_ => Lang.After<DistMap>(convergeTime, ReachabilityAssertionStable));
            
            var monolithicProperties = topology.ForAllNodes(_ => ReachabilityAssertionStable);
            
            return new Network<DistMap>(topology, topology.ForAllEdges(_ => TransferFunction), MergeFunction, initialValues, annotations,
                modularProperties, monolithicProperties);
        }

        private static Zen<bool> ReachabilityAssertionStable(Zen<DistMap> route)
        {
            // return route.Members.All();
            throw new NotImplementedException();
        }

        private static Zen<DistMap> MergeFunction(Zen<DistMap> r1, Zen<DistMap> r2)
        {
            // return r1.Zip(r2, Lang.Omap2<uint>(Min));
            throw new NotImplementedException();
        }

        private static Zen<DistMap> TransferFunction(Zen<DistMap> route)
        {
            // return route.ForEach(Lang.Omap(Lang.Incr(1)));
            throw new NotImplementedException();
        }
    }
}